# SplineMovement Component

## Overview

`SplineMovement` provides smooth rail/spline-based movement for 2.5D games using Catmull-Rom splines. Perfect for rail shooters, side-scrollers, racing games, and any game requiring movement along predefined curved paths.

## Features

- **Smooth Curved Paths**: Uses Catmull-Rom spline interpolation for natural curves
- **Closed Loop Support**: Create circular tracks by connecting the last point to the first
- **Lateral Offset**: Support for lane-based gameplay (e.g., move left/right in rail shooter)
- **Speed Control**: Manual input control or automatic constant speed
- **Bidirectional Movement**: Optional forward and backward movement
- **Auto-Rotation**: Automatically face the movement direction
- **Visual Debugging**: See the spline path and control points in Scene view

## Setup

### 1. Create Control Points

```csharp
// Create empty GameObjects to define the path
GameObject point1 = new GameObject("Point1");
GameObject point2 = new GameObject("Point2");
GameObject point3 = new GameObject("Point3");
GameObject point4 = new GameObject("Point4");

point1.transform.position = new Vector3(0, 0, 0);
point2.transform.position = new Vector3(5, 2, 0);
point3.transform.position = new Vector3(10, 1, 0);
point4.transform.position = new Vector3(15, 0, 0);
```

### 2. Create Actor with SplineMovement

```csharp
// Create actor
GameObject actor = new GameObject("RailShooterPlayer");
GameKitActor kitActor = actor.AddComponent<GameKitActor>();
kitActor.Initialize("player", GameKitActor.BehaviorProfile.SplineMovement, GameKitActor.ControlMode.DirectController);

// Add SplineMovement component
SplineMovement spline = actor.AddComponent<SplineMovement>();
spline.SetControlPoints(new Transform[] { point1.transform, point2.transform, point3.transform, point4.transform });
spline.StartMoving();
```

### 3. Configure Settings

Inspector properties:
- **Control Points**: Array of Transform objects defining the path
- **Closed Loop**: Connect last point to first for circular tracks
- **Resolution**: Segments per control point pair (higher = smoother)
- **Move Speed**: Speed along spline (units per second)
- **Auto Start**: Start moving automatically on `Start()`
- **Allow Manual Control**: Use input to control speed
- **Allow Backward Movement**: Enable reverse direction
- **Lateral Offset**: Offset from path for lane-based movement
- **Auto Rotate**: Face movement direction
- **Rotation Axis**: Which axis to rotate (Z for 2D sprites, All for 3D)

## Usage Examples

### Rail Shooter

```csharp
// Player moves along fixed path, can move laterally for dodging
public class RailShooterPlayer : MonoBehaviour
{
    private SplineMovement splineMovement;
    private GameKitActor actor;
    
    void Start()
    {
        splineMovement = GetComponent<SplineMovement>();
        actor = GetComponent<GameKitActor>();
        
        // Subscribe to input for lateral movement
        actor.OnMoveInput.AddListener(HandleLateralMovement);
        
        // Auto-advance along rail
        splineMovement.StartMoving();
    }
    
    void HandleLateralMovement(Vector3 input)
    {
        // Move left/right within bounds
        Vector3 offset = splineMovement.GetLateralOffset();
        offset.x += input.x * 5f * Time.deltaTime;
        offset.x = Mathf.Clamp(offset.x, -3f, 3f);
        splineMovement.SetLateralOffset(offset);
    }
}
```

### 2.5D Side-Scroller

```csharp
// Character follows winding path through environment
public class SideScrollerCharacter : MonoBehaviour
{
    private SplineMovement splineMovement;
    private GameKitActor actor;
    
    void Start()
    {
        splineMovement = GetComponent<SplineMovement>();
        actor = GetComponent<GameKitActor>();
        
        // Use input to control speed (forward/back)
        actor.OnMoveInput.AddListener(HandleMovementInput);
    }
    
    void HandleMovementInput(Vector3 input)
    {
        // Control speed with horizontal input
        if (input.x > 0.1f)
            splineMovement.StartMoving();
        else if (input.x < -0.1f && splineMovement.AllowBackwardMovement)
            splineMovement.ReverseDirection();
        else
            splineMovement.StopMoving();
    }
}
```

### Racing Game Track

```csharp
// Vehicle follows track with lane changes
public class RacingVehicle : MonoBehaviour
{
    private SplineMovement splineMovement;
    private int currentLane = 1; // 0=left, 1=center, 2=right
    private float laneWidth = 2.5f;
    
    void Start()
    {
        splineMovement = GetComponent<SplineMovement>();
        splineMovement.StartMoving();
    }
    
    public void ChangeLane(int direction)
    {
        currentLane = Mathf.Clamp(currentLane + direction, 0, 2);
        float targetX = (currentLane - 1) * laneWidth;
        splineMovement.SetLateralOffset(new Vector3(targetX, 0, 0));
    }
}
```

### Scripted Cutscene

```csharp
// Camera follows spline for cinematic sequence
public class CutsceneCamera : MonoBehaviour
{
    private SplineMovement splineMovement;
    
    IEnumerator PlayCutscene()
    {
        splineMovement = GetComponent<SplineMovement>();
        splineMovement.SetSpeed(3f);
        splineMovement.StartMoving();
        
        // Wait until camera reaches end of path
        while (splineMovement.Progress < 0.99f)
        {
            yield return null;
        }
        
        Debug.Log("Cutscene complete!");
    }
}
```

## API Reference

### Properties

- `float Progress` - Current position on spline (0 to 1)
- `bool IsMoving` - Whether movement is active
- `float CurrentSpeed` - Current movement speed
- `float SplineLength` - Total length of spline in world units

### Methods

- `void StartMoving()` - Start movement along spline
- `void StopMoving()` - Stop movement and reset speed
- `void SetProgress(float progress)` - Jump to position on spline (0-1)
- `void SetSpeed(float speed)` - Change movement speed
- `void SetLateralOffset(Vector3 offset)` - Set offset from path
- `void RebuildSpline()` - Recalculate spline from control points
- `void TeleportToProgress(float progress)` - Jump to position without speed
- `void ReverseDirection()` - Reverse movement direction (if allowed)
- `Vector3 GetPointAtProgress(float progress)` - Get world position at normalized progress
- `Vector3 GetTangentAtProgress(float progress)` - Get forward direction at progress
- `void SetControlPoints(Transform[] points)` - Set control points programmatically

## Best Practices

### Control Point Placement

1. **Spacing**: Keep control points evenly spaced for predictable speed
2. **Start/End**: First and last points affect curve shape; place extra points for control
3. **Sharp Turns**: Add more control points around corners for better curve quality
4. **Testing**: Visualize spline in Scene view during edit mode

### Performance

1. **Resolution**: Start with 10 segments per control point pair; increase if curves look jagged
2. **Control Points**: Use as few as necessary; more points = more computation
3. **Updates**: Spline is rebuilt on `Start()` and when properties change in editor

### Movement Feel

1. **Acceleration**: Use non-zero acceleration/deceleration for smoother starts/stops
2. **Speed**: Adjust speed based on path complexity (slower for tight curves)
3. **Rotation**: Match rotation axis to your game's orientation (Z for 2D, All for 3D)

## Common Patterns

### Lane-Based Rail Shooter

- Fixed forward speed
- Lateral offset for lane changes
- Input controls lateral movement only

### Free-Form 2.5D Platformer

- Manual speed control via input
- Forward/backward movement allowed
- Character animation tied to current speed

### Circular Track

- Closed loop enabled
- Auto-restart for continuous laps
- Progress tracking for lap counting

### Roller Coaster / Cutscene

- Auto-start enabled
- Auto-rotate to match path tangent
- Script controls when to start/stop

## Integration with GameKit

`SplineMovement` integrates with `GameKitActor`'s UnityEvents:

- `OnMoveInput` - Used for manual speed control (if enabled)
- `OnJumpInput` - Can trigger special actions (e.g., boost)
- `OnActionInput` - Custom gameplay actions while moving

## Troubleshooting

**Spline looks jagged:**
- Increase `resolution` in inspector
- Add more control points around curves

**Movement feels jerky:**
- Enable `acceleration` and `deceleration`
- Ensure `Time.timeScale` is 1.0

**Actor doesn't follow spline:**
- Check that `controlPoints` array is populated
- Call `RebuildSpline()` after changing control points at runtime
- Verify `moveSpeed` > 0 and `IsMoving` is true

**Rotation is wrong:**
- Adjust `rotationAxis` to match your game's orientation
- Disable `autoRotate` for manual control
- Check that spline tangent calculation is correct (visualize in Scene view)

## See Also

- [GameKit Actor](./README.md#gamekit-actor)
- [TileGridMovement](./TileGridMovement.README.md)
- [GraphNodeMovement](./GraphNodeMovement.README.md)

