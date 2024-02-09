using Godot;
using System;

public partial class PlayerCharacter : CharacterBody2D
{
	public const float Speed = 450.0f;
	public const float Deceleration = 15.0f;
	public const float AirDeceleration = 3.3f;
	public const float JumpVelocity = -460.0f;
	private const double CyoteTime = 0.1d;
	private const double teleportTimerReset = 0.3d;
	private const double clingTimerReset = 1.0d;
	private float dashSpeed = 1000.0f;
	private double cyoteTimer = CyoteTime;
	private double teleportTimer = teleportTimerReset;
	private double clingTimer = clingTimerReset;
	private bool doubleJumpAvailiable = true;
	private bool teleportAvailiable = true;

	public enum playerStates
	{
		grounded,
		airborn,
		clinging,
		teleporting
	}
	public playerStates PlayerState = playerStates.grounded;
	private Vector2 finalVelocity;
    private Vector2 direction;
	private Vector2 Stop = new Vector2(0,0);

	private AnimatedSprite2D sprite_2d;

	// Get the gravity from the project settings to be synced with RigidBody nodes.
	public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

	public override void _Ready()
	{
		base._Ready();
		sprite_2d = GetNode<AnimatedSprite2D>($"Sprite2D");
 	}
	public override void _PhysicsProcess(double delta)
	{
		finalVelocity = Velocity;
		
		switch(PlayerState)
		{
			case playerStates.grounded:
				doGroundedPhysics(ref finalVelocity, delta);
			break;
			case playerStates.airborn:
				doAirbornPhysics(ref finalVelocity, delta);
			break;
			case playerStates.clinging:
				doClingingPhysics(ref finalVelocity, delta);
			break;
			case playerStates.teleporting:
				doTeleportingPhysics(ref finalVelocity, delta);
			break;

			default:
			break;
		}	
		Velocity = finalVelocity;	
		MoveAndSlide();
	}
	private void doGroundedPhysics(ref Vector2 incomingVelocity, double incomingDelta)
	{
		//add gravity
		incomingVelocity.Y += gravity * (float)incomingDelta;

		//if touching ground, refresh cyote timer, if not, decrease it
		cyoteTimer = IsOnFloor() ? CyoteTime : -incomingDelta; 
				
		if(cyoteTimer == 0.0d)
		{
			teleportAvailiable = true;
			doubleJumpAvailiable = true;
			PlayerState = playerStates.airborn;
			GD.Print("entering Airborn State");
			return;
		}

		if (Input.IsActionJustPressed("jump"))
		{
			doubleJumpAvailiable = true;
			teleportAvailiable = true;
			PlayerState = playerStates.airborn;
			incomingVelocity.Y = JumpVelocity;
			cyoteTimer = 0.0d;
			GD.Print("entering Airborn State");
			return;

		}

		if (Input.IsActionJustPressed("dash"))
		{
			teleportAvailiable = false;
			doubleJumpAvailiable = true;
			PlayerState = playerStates.teleporting;
			teleportTimer = teleportTimerReset;
			GD.Print("entering Teleporting State");
			return;
		}

		// Get the input direction and handle the movement/deceleration.
		direction = Input.GetVector("left", "right", "up", "down");
		if (direction != Vector2.Zero)
		{
			//add the currently input dirrection to our  velocity
			incomingVelocity.X = Mathf.MoveToward(Velocity.X, direction.X * Speed, Deceleration);
			//turn the sprite to face the current inputted dirrection
			sprite_2d.FlipH = direction.X < 0;
		}
		else
		{
			incomingVelocity.X = Mathf.MoveToward(Velocity.X, 0, Deceleration);
		}
		if(incomingVelocity.X != 0.0f)
			sprite_2d.Animation = "running";
		else
			sprite_2d.Animation = "default";
			
	}

	private void doAirbornPhysics(ref Vector2 incomingVelocity, double incomingDelta)
	{
		//add gravity
		incomingVelocity.Y += gravity * (float)incomingDelta;

		if(IsOnFloor())
		{
			PlayerState = playerStates.grounded;
			doubleJumpAvailiable = true;
			teleportAvailiable = true;
			GD.Print("entering Grounded State");
			return;
		}
		if(IsOnWall())
		{
			incomingVelocity = Stop;
			clingTimer = clingTimerReset;
			//if they are on the wall they are clinging 
			PlayerState = playerStates.clinging;
			GD.Print("entering Clinging State");
			return;
		}

		if (Input.IsActionJustPressed("dash"))
		{
			if(teleportAvailiable)
			{
				PlayerState = playerStates.teleporting;
				teleportAvailiable = false;
				teleportTimer = teleportTimerReset;
				GD.Print("entering Teleporting State");
				return;
			}
		}

		if (Input.IsActionJustPressed("jump"))
		{		
			// if jump button is pressed, perform the jump function and set the result as the current velocity
			if (doubleJumpAvailiable)
			{
				incomingVelocity.Y = (JumpVelocity * 0.9f);
				doubleJumpAvailiable = false;
			}
		}

		direction = Input.GetVector("left", "right", "up", "down");
		// Get the input direction and handle the movement/deceleration.
		if (direction != Vector2.Zero)
		{
			//add the currently input dirrection to our  velocity
			incomingVelocity.X = Mathf.MoveToward(Velocity.X, direction.X * Speed, Deceleration);
			//turn the sprite to face the current inputted dirrection
			sprite_2d.FlipH = direction.X < 0;
		}
		else
		{
			finalVelocity.X = Mathf.MoveToward(Velocity.X, 0, AirDeceleration);
		}

		if(doubleJumpAvailiable)
			sprite_2d.Animation = "jumping";
		else
			sprite_2d.Animation = "doubleJump";
	}

	private void doClingingPhysics(ref Vector2 incomingVelocity, double incomingDelta)
	{
		if(IsOnFloor())
		{
			PlayerState = playerStates.grounded;
			doubleJumpAvailiable = true;
			teleportAvailiable = true;
			GD.Print("entering Grounded State");
			return;
		}
		//add gravity at 1/3 the normal value due to cat claws stuck in the wall we are clinging to
		if(clingTimer <= 0)
			incomingVelocity.Y += gravity / 3 * (float)incomingDelta;
		else
			clingTimer -= incomingDelta;

		if (Input.IsActionJustPressed("jump"))
		{
		}

	}

	private void doTeleportingPhysics(ref Vector2 incomingVelocity, double incomingDelta)
	{
		// do not add gravity. gravity does not apply to teleportation

		if(!sprite_2d.FlipH)
		{
			finalVelocity.X = dashSpeed;
			finalVelocity.Y = 0.0f;
		}
		else
		{
			finalVelocity.X = -dashSpeed;
			finalVelocity.Y = 0.0f;
		}

		if((teleportTimer -= incomingDelta)<=0.0d)
		{
			finalVelocity = Stop;
			if(IsOnFloor())
			{
				PlayerState = playerStates.grounded;
				GD.Print("entering Grounded State");
			}
			else if (IsOnWall())
			{	
				PlayerState = playerStates.clinging;
				GD.Print("entering Clinging State");
			}
			else
			{	
				PlayerState = playerStates.airborn;
				GD.Print("entering Airborn State");
			}
		}
	}

}
