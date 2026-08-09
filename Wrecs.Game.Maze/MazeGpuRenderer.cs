using System.Numerics;
using System.Runtime.InteropServices;
using SDL3;

namespace Wrecs.Game.Maze;

/// <summary>
/// Batches the maze's 2D primitives into a single SDL_GPU vertex buffer and draw call.
/// </summary>
sealed class MazeGpuRenderer : IDisposable
{
    private const int InitialVertexCapacity = 4096;
    private const int CircleSegments = 32;
    private const bool DebugMode =
#if DEBUG
        true;
#else
        false;
#endif

    private const string VertexShaderSource = """
        struct VertexInput
        {
            float2 Position : TEXCOORD0;
            float4 Color : TEXCOORD1;
        };

        struct VertexOutput
        {
            float4 Position : SV_Position;
            float4 Color : TEXCOORD0;
        };

        VertexOutput main(VertexInput input)
        {
            VertexOutput output;
            output.Position = float4(input.Position, 0.0f, 1.0f);
            output.Color = input.Color;
            return output;
        }
        """;

    private const string FragmentShaderSource = """
        float4 main(float4 position : SV_Position,
                    float4 color : TEXCOORD0) : SV_Target0
        {
            return color;
        }
        """;

    private readonly nint _window;
    private readonly int _logicalWidth;
    private readonly int _logicalHeight;
    private readonly List<GpuVertex> _vertices = new(InitialVertexCapacity);

    private nint _device;
    private nint _pipeline;
    private nint _sceneTexture;
    private nint _vertexBuffer;
    private nint _transferBuffer;
    private uint _bufferCapacityBytes;
    private SDL.GPUTextureFormat _targetFormat;
    private GpuColor _clearColor;
    private bool _shaderCrossInitialized;
    private bool _windowClaimed;
    private bool _disposed;

    public string DriverName => SDL.GetGPUDeviceDriver(_device) ?? "unknown";

    public MazeGpuRenderer(nint window, int logicalWidth, int logicalHeight)
    {
        _window = window;
        _logicalWidth = logicalWidth;
        _logicalHeight = logicalHeight;

        try
        {
            Initialize();
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public void BeginFrame(GpuColor clearColor)
    {
        _vertices.Clear();
        _clearColor = clearColor;
    }

    public void FillRectangle(float x,
                              float y,
                              float width,
                              float height,
                              GpuColor color)
    {
        var topLeft = new Vector2(x, y);
        var topRight = new Vector2(x + width, y);
        var bottomRight = new Vector2(x + width, y + height);
        var bottomLeft = new Vector2(x, y + height);

        AddTriangle(topLeft, topRight, bottomRight, color);
        AddTriangle(topLeft, bottomRight, bottomLeft, color);
    }

    public void FillCircle(Vector2 center, float radius, GpuColor color)
    {
        for (var segment = 0; segment < CircleSegments; segment++)
        {
            var angle0 = MathF.Tau * segment / CircleSegments;
            var angle1 = MathF.Tau * (segment + 1) / CircleSegments;
            var point0 = center + (new Vector2(MathF.Cos(angle0), MathF.Sin(angle0)) * radius);
            var point1 = center + (new Vector2(MathF.Cos(angle1), MathF.Sin(angle1)) * radius);
            AddTriangle(center, point0, point1, color);
        }
    }

    public void DrawLine(Vector2 start, Vector2 end, GpuColor color, float thickness = 1.5f)
    {
        var direction = end - start;
        if (direction.LengthSquared() <= float.Epsilon)
        {
            return;
        }

        var normal = Vector2.Normalize(new Vector2(-direction.Y, direction.X)) * (thickness / 2);
        var a = start + normal;
        var b = end + normal;
        var c = end - normal;
        var d = start - normal;

        AddTriangle(a, b, c, color);
        AddTriangle(a, c, d, color);
    }

    public unsafe void EndFrame()
    {
        ThrowIfDisposed();

        var commandBuffer = SDL.AcquireGPUCommandBuffer(_device);
        EnsureHandle(commandBuffer, "acquiring a GPU command buffer");

        if (!SDL.WaitAndAcquireGPUSwapchainTexture(commandBuffer,
                                                   _window,
                                                   out var swapchainTexture,
                                                   out var swapchainWidth,
                                                   out var swapchainHeight))
        {
            SDL.CancelGPUCommandBuffer(commandBuffer);
            throw SdlException("acquiring the GPU swapchain texture");
        }

        if (swapchainTexture == 0)
        {
            Submit(commandBuffer);
            return;
        }

        if (_vertices.Count > 0)
        {
            var dataSize = checked((uint)(_vertices.Count * sizeof(GpuVertex)));
            EnsureBufferCapacity(dataSize);

            var destination = SDL.MapGPUTransferBuffer(_device, _transferBuffer, true);
            EnsureHandle(destination, "mapping the GPU transfer buffer");

            var vertexSpan = CollectionsMarshal.AsSpan(_vertices);
            fixed (GpuVertex* source = vertexSpan)
            {
                Buffer.MemoryCopy(source, (void*)destination, _bufferCapacityBytes, dataSize);
            }
            SDL.UnmapGPUTransferBuffer(_device, _transferBuffer);

            var copyPass = SDL.BeginGPUCopyPass(commandBuffer);
            EnsureHandle(copyPass, "beginning the GPU copy pass");
            var sourceLocation = new SDL.GPUTransferBufferLocation
            {
                TransferBuffer = _transferBuffer,
                Offset = 0,
            };
            var destinationRegion = new SDL.GPUBufferRegion
            {
                Buffer = _vertexBuffer,
                Offset = 0,
                Size = dataSize,
            };
            SDL.UploadToGPUBuffer(copyPass, in sourceLocation, in destinationRegion, true);
            SDL.EndGPUCopyPass(copyPass);
        }

        var colorTarget = new SDL.GPUColorTargetInfo
        {
            Texture = _sceneTexture,
            ClearColor = _clearColor.ToSdlColor(),
            LoadOp = SDL.GPULoadOp.Clear,
            StoreOp = SDL.GPUStoreOp.Store,
        };
        var renderPass = SDL.BeginGPURenderPass(commandBuffer, [colorTarget], 1, 0);
        EnsureHandle(renderPass, "beginning the GPU render pass");

        if (_vertices.Count > 0)
        {
            SDL.BindGPUGraphicsPipeline(renderPass, _pipeline);
            var binding = new SDL.GPUBufferBinding
            {
                Buffer = _vertexBuffer,
                Offset = 0,
            };
            SDL.BindGPUVertexBuffers(renderPass, 0, [binding], 1);
            SDL.DrawGPUPrimitives(renderPass, (uint)_vertices.Count, 1, 0, 0);
        }

        SDL.EndGPURenderPass(renderPass);

        var blit = new SDL.GPUBlitInfo
        {
            Source = new SDL.GPUBlitRegion
            {
                Texture = _sceneTexture,
                W = (uint)_logicalWidth,
                H = (uint)_logicalHeight,
            },
            Destination = new SDL.GPUBlitRegion
            {
                Texture = swapchainTexture,
                W = swapchainWidth,
                H = swapchainHeight,
            },
            LoadOp = SDL.GPULoadOp.DontCare,
            Filter = SDL.GPUFilter.Linear,
        };
        SDL.BlitGPUTexture(commandBuffer, in blit);
        Submit(commandBuffer);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_device != 0)
        {
            SDL.WaitForGPUIdle(_device);
            SDL.ReleaseGPUTransferBuffer(_device, _transferBuffer);
            SDL.ReleaseGPUBuffer(_device, _vertexBuffer);
            SDL.ReleaseGPUGraphicsPipeline(_device, _pipeline);
            SDL.ReleaseGPUTexture(_device, _sceneTexture);
            if (_windowClaimed)
            {
                SDL.ReleaseWindowFromGPUDevice(_device, _window);
            }
            SDL.DestroyGPUDevice(_device);
        }

        if (_shaderCrossInitialized)
        {
            ShaderCross.Quit();
        }
    }

    private unsafe void Initialize()
    {
        if (!ShaderCross.Init())
        {
            throw SdlException("initializing SDL_shadercross");
        }
        _shaderCrossInitialized = true;

        var shaderFormats = ShaderCross.GetHLSLShaderFormats();
        var preferredDriver = OperatingSystem.IsWindows()
            ? "direct3d12"
            : OperatingSystem.IsMacOS()
                ? "metal"
                : null;
        _device = SDL.CreateGPUDevice(shaderFormats,
                                      DebugMode,
                                      preferredDriver);
        EnsureHandle(_device, "creating the SDL_GPU device");

        if (!SDL.ClaimWindowForGPUDevice(_device, _window))
        {
            throw SdlException("claiming the window for the SDL_GPU device");
        }
        _windowClaimed = true;

        _targetFormat = SDL.GetGPUSwapchainTextureFormat(_device, _window);
        var textureInfo = new SDL.GPUTextureCreateInfo
        {
            Type = SDL.GPUTextureType.TextureType2D,
            Format = _targetFormat,
            Usage = SDL.GPUTextureUsageFlags.ColorTarget | SDL.GPUTextureUsageFlags.Sampler,
            Width = (uint)_logicalWidth,
            Height = (uint)_logicalHeight,
            LayerCountOrDepth = 1,
            NumLevels = 1,
            SampleCount = SDL.GPUSampleCount.SampleCount1,
        };
        _sceneTexture = SDL.CreateGPUTexture(_device, in textureInfo);
        EnsureHandle(_sceneTexture, "creating the GPU scene texture");

        var vertexShader = CompileShader(VertexShaderSource, ShaderCross.ShaderStage.Vertex);
        var fragmentShader = CompileShader(FragmentShaderSource, ShaderCross.ShaderStage.Fragment);

        try
        {
            var bufferDescriptions = new[]
            {
                new SDL.GPUVertexBufferDescription
                {
                    Slot = 0,
                    Pitch = (uint)sizeof(GpuVertex),
                    InputRate = SDL.GPUVertexInputRate.Vertex,
                },
            };
            var attributes = new[]
            {
                new SDL.GPUVertexAttribute
                {
                    Location = 0,
                    BufferSlot = 0,
                    Format = SDL.GPUVertexElementFormat.Float2,
                    Offset = 0,
                },
                new SDL.GPUVertexAttribute
                {
                    Location = 1,
                    BufferSlot = 0,
                    Format = SDL.GPUVertexElementFormat.Float4,
                    Offset = 2 * sizeof(float),
                },
            };
            var colorTargets = new[]
            {
                new SDL.GPUColorTargetDescription
                {
                    Format = _targetFormat,
                },
            };

            fixed (SDL.GPUVertexBufferDescription* bufferDescriptionsPointer = bufferDescriptions)
            fixed (SDL.GPUVertexAttribute* attributesPointer = attributes)
            fixed (SDL.GPUColorTargetDescription* colorTargetsPointer = colorTargets)
            {
                var pipelineInfo = new SDL.GPUGraphicsPipelineCreateInfo
                {
                    VertexShader = vertexShader,
                    FragmentShader = fragmentShader,
                    VertexInputState = new SDL.GPUVertexInputState
                    {
                        VertexBufferDescriptions = (nint)bufferDescriptionsPointer,
                        NumVertexBuffers = (uint)bufferDescriptions.Length,
                        VertexAttributes = (nint)attributesPointer,
                        NumVertexAttributes = (uint)attributes.Length,
                    },
                    PrimitiveType = SDL.GPUPrimitiveType.TriangleList,
                    RasterizerState = new SDL.GPURasterizerState
                    {
                        FillMode = SDL.GPUFillMode.Fill,
                        CullMode = SDL.GPUCullMode.None,
                        FrontFace = SDL.GPUFrontFace.CounterClockwise,
                    },
                    MultisampleState = new SDL.GPUMultisampleState
                    {
                        SampleCount = SDL.GPUSampleCount.SampleCount1,
                    },
                    TargetInfo = new SDL.GPUGraphicsPipelineTargetInfo
                    {
                        ColorTargetDescriptions = (nint)colorTargetsPointer,
                        NumColorTargets = (uint)colorTargets.Length,
                    },
                };

                _pipeline = SDL.CreateGPUGraphicsPipeline(_device, in pipelineInfo);
            }

            EnsureHandle(_pipeline, "creating the GPU graphics pipeline");
        }
        finally
        {
            SDL.ReleaseGPUShader(_device, vertexShader);
            SDL.ReleaseGPUShader(_device, fragmentShader);
        }

        EnsureBufferCapacity((uint)(InitialVertexCapacity * sizeof(GpuVertex)));
    }

    private nint CompileShader(string source, ShaderCross.ShaderStage stage)
    {
        var hlslInfo = new ShaderCross.HLSLInfo
        {
            ManagedSource = source,
            ManagedEntrypoint = "main",
            ShaderStage = stage,
        };
        var spirvByteCode = ShaderCross.CompileSPIRVFromHLSL(ref hlslInfo, out var spirvSize);
        EnsureHandle(spirvByteCode, $"compiling the {stage} shader to SPIR-V");

        try
        {
            var spirvInfo = new ShaderCross.SPIRVInfo
            {
                ByteCode = spirvByteCode,
                ByteCodeSize = spirvSize,
                ManagedEntrypoint = "main",
                ShaderStage = stage,
            };
            var resourceInfo = new ShaderCross.GraphicsShaderResourceInfo();
            var shader = ShaderCross.CompileGraphicsShaderFromSPIRV(_device,
                                                                    ref spirvInfo,
                                                                    ref resourceInfo,
                                                                    0);
            EnsureHandle(shader, $"cross-compiling the {stage} shader for the active GPU backend");
            return shader;
        }
        finally
        {
            SDL.Free(spirvByteCode);
        }
    }

    private void EnsureBufferCapacity(uint requiredBytes)
    {
        if (requiredBytes <= _bufferCapacityBytes)
        {
            return;
        }

        var newCapacity = Math.Max(requiredBytes, Math.Max(256u, _bufferCapacityBytes * 2));
        SDL.ReleaseGPUTransferBuffer(_device, _transferBuffer);
        SDL.ReleaseGPUBuffer(_device, _vertexBuffer);

        var bufferInfo = new SDL.GPUBufferCreateInfo
        {
            Usage = SDL.GPUBufferUsageFlags.Vertex,
            Size = newCapacity,
        };
        _vertexBuffer = SDL.CreateGPUBuffer(_device, in bufferInfo);
        EnsureHandle(_vertexBuffer, "creating the GPU vertex buffer");

        var transferInfo = new SDL.GPUTransferBufferCreateInfo
        {
            Usage = SDL.GPUTransferBufferUsage.Upload,
            Size = newCapacity,
        };
        _transferBuffer = SDL.CreateGPUTransferBuffer(_device, in transferInfo);
        EnsureHandle(_transferBuffer, "creating the GPU transfer buffer");
        _bufferCapacityBytes = newCapacity;
    }

    private void AddTriangle(Vector2 a, Vector2 b, Vector2 c, GpuColor color)
    {
        _vertices.Add(new(ToClipSpace(a), color));
        _vertices.Add(new(ToClipSpace(b), color));
        _vertices.Add(new(ToClipSpace(c), color));
    }

    private Vector2 ToClipSpace(Vector2 point) => new((point.X / _logicalWidth * 2) - 1,
                                                       1 - (point.Y / _logicalHeight * 2));

    private static void EnsureHandle(nint handle, string operation)
    {
        if (handle == 0)
        {
            throw SdlException(operation);
        }
    }

    private static InvalidOperationException SdlException(string operation) =>
        new($"SDL failed while {operation}: {SDL.GetError()}");

    private static void Submit(nint commandBuffer)
    {
        if (!SDL.SubmitGPUCommandBuffer(commandBuffer))
        {
            throw SdlException("submitting the GPU command buffer");
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct GpuVertex(Vector2 Position, GpuColor Color);
}

[StructLayout(LayoutKind.Sequential)]
readonly record struct GpuColor(float Red, float Green, float Blue, float Alpha = 1)
{
    public static GpuColor FromBytes(byte red, byte green, byte blue, byte alpha = 255) =>
        new(red / 255f, green / 255f, blue / 255f, alpha / 255f);

    public SDL.FColor ToSdlColor() => new()
    {
        R = Red,
        G = Green,
        B = Blue,
        A = Alpha,
    };
}
