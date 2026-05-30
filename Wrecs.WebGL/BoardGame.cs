using System.Runtime.InteropServices.JavaScript;
using System.Text;
using ThoughtStuff.GLSourceGen;
using Wrecs.Tests;
using Wrecs.WebGL.Examples;
using Wrecs.WebGL.Examples.InstanceParticles;
using Wrecs.WebGL.GameFramework;

namespace Wrecs.WebGL;

sealed partial class BoardGame : IGame, IDisposable
{
    private readonly SimplestBoardGame _game = new();
    private JSObject? _shaderProgram;
    private JSObject? _vertexBuffer;
    private JSObject? _instanceBuffer;
    private readonly List<JSObject> _textures = [];
    private readonly List<JSObject> _buffers = [];
    private List<int[]> _frameSetIndices = [[0]];
    private readonly List<int> _vertexAttributeLocations = [];
    // private int _fpsMin = 24;
    // private int _fpsMax = 24;

    public string? OverlayText
    {
        get
        {
            var sb = new StringBuilder();
            sb.AppendLine("Simplest Board Game");
            sb.AppendLine("Player 1 at: " + _game.GetPosition(_game.Player1));
            sb.AppendLine("Player 2 at: " + _game.GetPosition(_game.Player2));
            sb.AppendLine("Current Player: " + _game.GetCurrentPlayer().Name);
            sb.AppendLine("Game Over: " + _game.IsGameOver());
            sb.AppendLine("Winner: " + _game.WinnerName());
            return sb.ToString();
        }
    }

    /// <inheritdoc/>
    public async Task LoadAssetsEssentialAsync(IShaderLoader shaderLoader, ITextureLoader textureLoader)
    {
        // Load the shader program
        _shaderProgram = shaderLoader.LoadShaderProgram("Transformed2D/SpriteSheet_vert",
                                                        "Basic/TextureUnlit_frag");

        // Load and bind texture (low-res for initial load)
        var lowResTexture = await textureLoader.LoadTexture("/textures/arrows-lowres.png");
        GL.ActiveTexture(GL.TEXTURE0);
        GL.BindTexture(GL.TEXTURE_2D, lowResTexture);
        var textureUniformLoc = GL.GetUniformLocation(_shaderProgram, "u_Texture");
        GL.Uniform1i(textureUniformLoc, 0);
        _textures.Add(lowResTexture);

        // Sprite Sheet parameters (see textures/arrows.json)
        int columnCount = 8;
        int rowCount = 15;
        float paddingRight = (512 - 480) / 512f;
        float paddingBottom = (1024 - 900) / 1024f;
        var blackArrow = Enumerable.Range(0, 59);
        var blueArrow = Enumerable.Range(59, 58);
        _frameSetIndices = [
            blackArrow.ToArray(),
            blackArrow.Reverse().ToArray(),
            blueArrow.ToArray(),
            blueArrow.Reverse().ToArray()
        ];
        // _fpsMin = 24;
        // _fpsMax = 120;

        // Setup Sprite Sheet parameters as shader Uniforms
        var uSpriteSheetColumnCountLocation = GL.GetUniformLocation(_shaderProgram, "u_SpriteSheetColumnCount");
        var uSpriteSheetRowCountLocation = GL.GetUniformLocation(_shaderProgram, "u_SpriteSheetRowCount");
        var uPaddingRightLoc = GL.GetUniformLocation(_shaderProgram, "u_PaddingRight");
        var uPaddingBottomLoc = GL.GetUniformLocation(_shaderProgram, "u_PaddingBottom");
        GL.Uniform1f(uSpriteSheetColumnCountLocation, columnCount);
        GL.Uniform1f(uSpriteSheetRowCountLocation, rowCount);
        GL.Uniform1f(uPaddingRightLoc, paddingRight);
        GL.Uniform1f(uPaddingBottomLoc, paddingBottom);
    }

    [SetupVertexAttrib("Shaders/Transformed2D/SpriteSheet_vert.glsl")]
    partial void BindVertexBufferData(JSObject shaderProgram,
                                      JSObject vertexBuffer,
                                      Span<TextureVertex2> vertices,
                                      List<int> vertexAttributeLocations);

    [SetupVertexAttrib("Shaders/Transformed2D/SpriteSheet_vert.glsl")]
    partial void BindVertexBufferData(JSObject shaderProgram,
                                      JSObject vertexBuffer,
                                      Span<InstanceData> vertices,
                                      List<int> vertexAttributeLocations);

    /// <inheritdoc/>
    public void InitializeScene(IShaderLoader shaderLoader)
    {
        if (_shaderProgram is null)
            throw new InvalidOperationException("Shader program not initialized");

        // Define quad vertices with positions and texture coordinates
        Span<TextureVertex2> vertices =
        [
            new(new(-0.5f, 0.5f), new(0.0f, 1.0f)), // Top-left
            new(new(0.5f, 0.5f), new(1.0f, 1.0f)), // Top-right
            new(new(-0.5f, -0.5f), new(0.0f, 0.0f)), // Bottom-left
            new(new(0.5f, 0.5f), new(1.0f, 1.0f)), // Top-right
            new(new(0.5f, -0.5f), new(1.0f, 0.0f)), // Bottom-right
            new(new(-0.5f, -0.5f), new(0.0f, 0.0f)) // Bottom-left
        ];

        // Create and bind the position and texture buffer for the quad vertices
        _vertexBuffer = GL.CreateBuffer();
        _buffers.Add(_vertexBuffer);
        BindVertexBufferData(_shaderProgram, _vertexBuffer, vertices, _vertexAttributeLocations);

        // Create a buffer for instance data (transformation and sprite frame index)
        _instanceBuffer = GL.CreateBuffer();
        _buffers.Add(_instanceBuffer);
        Span<InstanceData> instanceData = stackalloc InstanceData[1];
        List<int> instanceAttribLocations = [];
        BindVertexBufferData(_shaderProgram, _instanceBuffer, instanceData, instanceAttribLocations);
        foreach (var location in instanceAttribLocations)
        {
            // Set divisor to 1 so that the buffer is treated as instance data (1 per particle)
            GL.VertexAttribDivisor(location, 1);
        }
        _vertexAttributeLocations.AddRange(instanceAttribLocations);

        // Enable alpha blending for the textures which have an alpha channel
        GL.Enable(GL.BLEND);
        GL.BlendFunc(GL.SRC_ALPHA, GL.ONE_MINUS_SRC_ALPHA);

        GL.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
    }

    /// <inheritdoc/>
    public async Task LoadAssetsExtendedAsync(IShaderLoader shaderLoader, ITextureLoader textureLoader)
    {
        // Load the high-res texture
        string texturePath = "/textures/arrows.png";
        var highResTexture = await textureLoader.LoadTexture(texturePath);
        GL.ActiveTexture(GL.TEXTURE0);
        GL.BindTexture(GL.TEXTURE_2D, highResTexture);

        // Delete the low-res texture
        if (_textures.Count > 0)
        {
            var lowResTexture = _textures[0];
            GL.DeleteTexture(lowResTexture);
            lowResTexture.Dispose();
            _textures.Clear();
        }
        _textures.Add(highResTexture);
    }

    public void Dispose()
    {
        // Restore default settings
        GL.Disable(GL.BLEND);

        // Disable all vertex attribute locations
        foreach (var attributeLocation in _vertexAttributeLocations)
        {
            GL.DisableVertexAttribArray(attributeLocation);
        }
        _vertexAttributeLocations.Clear();

        // Dispose of all buffer objects
        _vertexBuffer = null;
        _instanceBuffer = null;
        foreach (var buffer in _buffers)
        {
            if (buffer is not null)
            {
                GL.DeleteBuffer(buffer);
                buffer.Dispose();
            }
        }
        _buffers.Clear();

        // Dispose of all texture objects
        foreach (var texture in _textures)
        {
            if (texture is not null)
            {
                GL.DeleteTexture(texture);
                texture.Dispose();
            }
        }
        _textures.Clear();

        // Dispose of the shader program
        if (_shaderProgram is not null)
            ShaderLoader.DisposeShaderProgram(_shaderProgram);
        _shaderProgram = null;
    }

    /// <inheritdoc/>
    public void Render()
    {
        GL.Clear(GL.COLOR_BUFFER_BIT);

        if (_instanceBuffer is null || _shaderProgram is null || _vertexBuffer is null)
            return;

        var players = new[] { _game.Player1, _game.Player2 };
        var particleCount = players.Length;
        Span<InstanceData> instanceData = stackalloc InstanceData[particleCount];
        for (int i = 0; i < particleCount; i++)
        {
            var frameIndex = 0;  //_particles[i].FrameIndex;
            // var transform = Matrix3x2.CreateRotation(_particles[i].Rotation) *
            //                 Matrix3x2.CreateScale(_particles[i].Scale) *
            //                 Matrix3x2.CreateTranslation(_particles[i].Position);
            var position = new Vector2(_game.GetPosition(players[i]) / (float)SimplestBoardGame.BoardSize * 2 - 1, 0);
            var transform = Matrix3x2.CreateTranslation(position);
            instanceData[i] = new InstanceData(transform, frameIndex);
        }
        // Update the instance VBO with the latest data
        GL.BindBuffer(GL.ARRAY_BUFFER, _instanceBuffer);
        GL.BufferData(GL.ARRAY_BUFFER, instanceData, GL.STREAM_DRAW);

        // Bind the vertex buffer for the quad
        GL.BindBuffer(GL.ARRAY_BUFFER, _vertexBuffer);

        // Draw all particles in a single call
        GL.DrawArraysInstanced(GL.TRIANGLES, 0, 6, particleCount);
    }

    /// <inheritdoc/>
    public void Update(TimeSpan deltaTime) { }

    /// <inheritdoc/>
    public void FixedUpdate(TimeSpan deltaTime) { }

    /// <inheritdoc/>
    public void OnKeyPress(string key, bool pressed) { }

    /// <inheritdoc/>
    public void OnMouseClick(int button, bool pressed, Vector2 position)
    {
        if (pressed)
        {
            if (_game.IsGameOver())
                _game.Reset();
            else
                _game.Tick();
            Console.WriteLine("Tick! Current Player: " + _game.GetCurrentPlayer().Name);
        }
    }

    /// <inheritdoc/>
    public void OnMouseMove(Vector2 position) { }

    /// <inheritdoc/>
    public void OnTouchStart(IEnumerable<Vector2> touches) { }

    /// <inheritdoc/>
    public void OnTouchMove(IEnumerable<Vector2> touches) => OnTouchStart(touches);

    /// <inheritdoc/>
    public void OnTouchEnd(IEnumerable<Vector2> touches) => OnTouchStart(touches);
}
