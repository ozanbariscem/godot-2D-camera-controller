using Godot;
using System;

public class CameraController : Camera2D
{
    public static event Action OnZoom;
    public static event Action OnMove;

    private float targetZoom;
    private Vector2 targetPosition;

    [Export] private float MovementSpeed = 5f;
    [Export] private float ZoomSpeed = 5f;

    [Export] private float MouseZoomSpeed = 5f;

    [Export] private float KeyboardMovementSpeed = 5f;
    [Export] private float KeyboardZoomSpeed = 5f;

    [Export] private float BorderMovementSpeed = 5f;
    [Export] private float BorderMovementRange = 0.05f;

    [Export] private Vector2 ZoomLimit = new Vector2(.5f, 2f);
    [Export] private Vector2 MinBounds = new Vector2(0, 0);
    [Export] private Vector2 MaxBounds = new Vector2(0, 0);

    private float CameraZoomSlow => SlowCameraWithZoom ? SlowCameraCurve.Interpolate(Zoom.x / ZoomLimit.x) : 1;

    [Export] private Curve SlowCameraCurve;
    [Export] private bool SlowCameraWithZoom;

    [Export] private bool SmoothZoom;
    [Export] private bool KeyboardZoom;
    [Export] private bool MouseZoom;

    [Export] private bool BorderMovement;
    [Export] private bool KeyboardMovement;
    [Export] private bool MouseMovement;
    [Export] private bool SmoothMovement;

    private Viewport viewport;

    private bool IgnoreAllInput;

    private bool isDragging;
    private Vector2 dragStart;
    private Vector2 dragStartPosition;

    public override void _Ready()
    {
        Current = true;
        viewport = GetViewport();
        RegionManager.OnMapCreated += HandleMapCreated;
    }

    public override void _ExitTree()
    {
        RegionManager.OnMapCreated -= HandleMapCreated;
    }

    public override void _Process(float delta)
    {
        HandleInput();

        ZoomToTarget(targetZoom, delta);
        MoveToTarget(targetPosition, delta);
    }

    public override void _Input(InputEvent @event)
    {
        if (IgnoreAllInput) return;
        if (!MouseZoom) return;

        if (@event is InputEventMouseButton mouseEvent)
        {
            int direction = 0;
            if ((ButtonList)mouseEvent.ButtonIndex == ButtonList.WheelUp)
                direction = -1;
            if ((ButtonList)mouseEvent.ButtonIndex == ButtonList.WheelDown)
                direction = 1;
            if (direction != 0) ZoomInput(direction, MouseZoomSpeed);
        }
    }

    private void HandleInput()
    {
        if (IgnoreAllInput) return;

        HandleKeyboardEvent();
        HandleMouseButtonEvent();
        HandleMousePosition();
        HandleDrag();
    }

    private void HandleKeyboardEvent()
    {
        if (KeyboardMovement)
        {
            Vector2 vector = new Vector2(0, 0);
            if (Input.IsKeyPressed((int)KeyList.W))
            {
                vector.y += -1;
            }
            if (Input.IsKeyPressed((int)KeyList.S))
            {
                vector.y += 1;
            }
            if (Input.IsKeyPressed((int)KeyList.D))
            {
                vector.x += 1;
            }
            if (Input.IsKeyPressed((int)KeyList.A))
            {
                vector.x -= 1;
            }
            MoveInput(vector, KeyboardMovementSpeed);
        }
        
        if (KeyboardZoom)
        {
            if (Input.IsKeyPressed((int)KeyList.R))
            {
                ZoomInput(-1, KeyboardZoomSpeed);
            }
            if (Input.IsKeyPressed((int)KeyList.F))
            {
                ZoomInput(1, KeyboardZoomSpeed);
            }
        }
    }

    private void HandleMouseButtonEvent()
    {
        if (MouseMovement)
        {
            if (Input.IsMouseButtonPressed(3))
            {
                // if we just started dragging
                if (!isDragging)
                {
                    dragStartPosition = targetPosition;
                    dragStart = GetGlobalMousePosition() + GetLocalMousePosition();
                    isDragging = true;
                }
            } else
            {
                isDragging = false;
            }
        }
    }

    private void HandleMousePosition()
    {
        if (!BorderMovement) return;
        Vector2 position = viewport.GetMousePosition();
        Vector2 vector = new Vector2(0, 0);
        if (position.x < viewport.Size.x * BorderMovementRange)
            vector.x = -1;
        if (position.y < viewport.Size.y * BorderMovementRange)
            vector.y = -1;
        if (position.x > viewport.Size.x - viewport.Size.x * BorderMovementRange)
            vector.x = 1;
        if (position.y > viewport.Size.y - viewport.Size.y * BorderMovementRange)
            vector.y = 1;
        MoveInput(vector, BorderMovementSpeed);
    }

    private void HandleDrag()
    {
        if (!MouseMovement) return;
        if (!isDragging) return;

        Vector2 difference = dragStart - GetGlobalMousePosition() - GetLocalMousePosition(); // Mouse moved x pixels?
        float distance = dragStart.DistanceTo(GetGlobalMousePosition());
        targetPosition = dragStartPosition + difference; // Add the pixels to the position where we started the drag
    }

    private void MoveInput(Vector2 value, float multiplier)
    {
        targetPosition += value * multiplier * GetProcessDeltaTime() * CameraZoomSlow;
        targetPosition = new Vector2(
            Mathf.Clamp(targetPosition.x, MinBounds.x, MaxBounds.x),
            Mathf.Clamp(targetPosition.y, MinBounds.y, MaxBounds.y));
        targetPosition = new Vector2(Mathf.RoundToInt(targetPosition.x), Mathf.RoundToInt(targetPosition.y));
    }

    private void ZoomInput(float value, float multiplier)
    {
        targetZoom += value * multiplier * GetProcessDeltaTime() * CameraZoomSlow;
        targetZoom = Mathf.Clamp(targetZoom, ZoomLimit.x, ZoomLimit.y);
    }

    public void ZoomToTarget(float target, float delta)
    {
        target = Mathf.Clamp(target, ZoomLimit.x, ZoomLimit.y);
        // No point in updating
        // We could also check for state changes somewhere else and only call this function incase of a change
        if (Mathf.Abs(Zoom.x - target) < 0.01f) return;

        if (SmoothZoom)
        {
            Zoom = Zoom.LinearInterpolate(new Vector2(target, target), ZoomSpeed * delta);
        } else
        {
            Zoom = new Vector2(target, target);
        }
        OnZoom?.Invoke();
    }

    public void MoveToTarget(Vector2 target, float delta)
    {
        // No point in updating
        // We could also check for state changes somewhere else and only call this function incase of a change
        if (GlobalPosition.DistanceTo(target) < 0.01f) return;
        
        if (SmoothMovement)
        {
            GlobalPosition = GlobalPosition.LinearInterpolate(target, MovementSpeed * delta);
            GlobalPosition = new Vector2(Mathf.RoundToInt(GlobalPosition.x), Mathf.RoundToInt(GlobalPosition.y));
        } else
        {
            GlobalPosition = target;
        }
        OnMove?.Invoke();
    }

    private void HandleMapCreated(Vector2 size, Vector2 center, byte width, byte height)
    {
        MinBounds = new Vector2(0, 0);
        MaxBounds = new Vector2((int)size.x, (int)size.y);
    }
}
