using System.Runtime.InteropServices.JavaScript;
using System.Text;
using Wrecs.Tests;
using Wrecs.WebGL.GameFramework;

namespace Wrecs.WebGL;

sealed class BoardGame : IGame, IDisposable
{
    private JSObject? _positionBuffer;
    private JSObject? _colorBuffer;
    private JSObject? _shaderProgram;
    private readonly List<int> _vertexAttributeLocations = [];

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

    private readonly SimplestBoardGame _game = new();

    /// <inheritdoc/>
    public Task LoadAssetsEssentialAsync(IShaderLoader shaderLoader, ITextureLoader textureLoader)
    {
        // Load low-res textures here for the initial render
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void InitializeScene(IShaderLoader shaderLoader)
    {
        // Load the shader program
        _shaderProgram = shaderLoader.LoadShaderProgram("Basic/ColorPassthrough_vert", "Basic/ColorPassthrough_frag");

        // POSITIONS
        // Create a buffer for the triangle's vertex positions.
        _positionBuffer = GL.CreateBuffer();
        GL.BindBuffer(GL.ARRAY_BUFFER, _positionBuffer);
        // Define the vertex positions for the triangle.
        Span<float> positions =
        [
            0.0f, 1.0f,
            -1.0f, -1.0f,
            1.0f, -1.0f
        ];
        GL.BufferData(GL.ARRAY_BUFFER, positions, GL.STATIC_DRAW);
        // Tell WebGL how to pull out the positions from the position buffer into the vertexPosition attribute.
        var positionAttributeLocation = GL.GetAttribLocation(_shaderProgram, "a_VertexPosition");
        GL.VertexAttribPointer(positionAttributeLocation, 2, GL.FLOAT, false, 0, 0);
        GL.EnableVertexAttribArray(positionAttributeLocation);
        _vertexAttributeLocations.Add(positionAttributeLocation);

        // COLORS
        // Create a buffer for the triangle's colors.
        _colorBuffer = GL.CreateBuffer();
        GL.BindBuffer(GL.ARRAY_BUFFER, _colorBuffer);
        // Define the colors for each vertex of the triangle (Rainbow: Red, Green, Blue).
        Span<float> colors =
        [
            1.0f, 0.0f, 0.0f, 1.0f, // Red
            0.0f, 1.0f, 0.0f, 1.0f, // Green
            0.0f, 0.0f, 1.0f, 1.0f  // Blue
        ];
        GL.BufferData(GL.ARRAY_BUFFER, colors, GL.STATIC_DRAW);
        // Tell WebGL how to pull out the colors from the color buffer into the vertexColor attribute.
        var colorAttributeLocation = GL.GetAttribLocation(_shaderProgram, "a_VertexColor");
        GL.VertexAttribPointer(colorAttributeLocation, 4, GL.FLOAT, false, 0, 0);
        GL.EnableVertexAttribArray(colorAttributeLocation);
        _vertexAttributeLocations.Add(colorAttributeLocation);

        // Set the clear color to cornflower blue
        GL.ClearColor(0.39f, 0.58f, 0.93f, 1.0f);
        GL.Clear(GL.COLOR_BUFFER_BIT);
    }

    /// <inheritdoc/>
    public Task LoadAssetsExtendedAsync(IShaderLoader shaderLoader, ITextureLoader textureLoader)
    {
        // Load high-res textures here for full fidelity
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // Disable all vertex attribute locations
        foreach (var attributeLocation in _vertexAttributeLocations)
        {
            GL.DisableVertexAttribArray(attributeLocation);
        }
        _vertexAttributeLocations.Clear();

        // Release WebGL resources
        if (_colorBuffer is not null)
        {
            GL.DeleteBuffer(_colorBuffer);
            _colorBuffer.Dispose();
            _colorBuffer = null;
        }
        if (_positionBuffer is not null)
        {
            GL.DeleteBuffer(_positionBuffer);
            _positionBuffer.Dispose();
            _positionBuffer = null;
        }
        if (_shaderProgram is not null)
            ShaderLoader.DisposeShaderProgram(_shaderProgram);
        _shaderProgram = null;
    }

    /// <inheritdoc/>
    public void Update(TimeSpan deltaTime)
    {
        if (_positionBuffer is null)
            return;

        var boardSize = SimplestBoardGame.BoardSize;
        var p1 = _game.GetPosition(_game.Player1) / (float)boardSize; // Normalize player position to [0, 1] range
        // Map player position to [-1, 1] range for WebGL
        var x1 = p1 * 2 - 1;
        var y1 = 0.25f;

        var p2 = _game.GetPosition(_game.Player2) / (float)boardSize; // Normalize player position to [0, 1] range
        var x2 = p2 * 2 - 1;
        var y2 = -0.25f;

        Span<float> positions =
        [
            x1, y1,
            x2, y2,
            0, -1f
        ];
        GL.BindBuffer(GL.ARRAY_BUFFER, _positionBuffer);
        GL.BufferData(GL.ARRAY_BUFFER, positions, GL.STATIC_DRAW);
    }

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

    /// <inheritdoc/>
    public void Render()
    {
        GL.Clear(GL.COLOR_BUFFER_BIT);
        if (_positionBuffer is not null)
        {
            GL.DrawArrays(GL.TRIANGLES, 0, 3);
        }
    }
}
