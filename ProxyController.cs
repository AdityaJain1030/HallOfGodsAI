using Modding;
using InControl;

namespace HallOfGodsAI.Utils
{
	   public class InputDeviceShim : InputDevice
    {
		// private bool[] Movement = new bool[4] {false, false, false, false};
		private bool Up = false;
		private bool Down = false;
		private bool Left = false;
		private bool Right = false;
		private bool Jump = false;
		private bool Attack = false;
		private bool Dash = false;
		private bool Cast = false;


		// public bool[] Actions = [false, false, false, false, false];

        public InputDeviceShim() :
            base("CustomInputShimDevice")
        {
			AddControl(InputControlType.DPadUp, "Up");
			AddControl(InputControlType.DPadDown, "Down");
			AddControl(InputControlType.DPadLeft, "Left");
			AddControl(InputControlType.DPadRight, "Right");
			AddControl(InputControlType.Action1, "Jump");
			AddControl(InputControlType.Action2, "Cast"); // Hold for heal
			AddControl(InputControlType.Action3, "Attack");
			AddControl(InputControlType.Action4, "DreamNail");
			AddControl(InputControlType.Action4, "DreamNail");
			AddControl(InputControlType.RightTrigger, "Dash");

			AddControl(InputControlType.LeftTrigger, "SuperDash");
			AddControl(InputControlType.RightBumper, "QuickCast");
        }
		public override void Update(ulong updateTick, float deltaTime)
		{
			// base.Update(updateTick, deltaTime);
			UpdateWithState(InputControlType.DPadUp, Up, updateTick, deltaTime);
			UpdateWithState(InputControlType.DPadDown, Down, updateTick, deltaTime);
			UpdateWithState(InputControlType.DPadLeft, Left, updateTick, deltaTime);
			UpdateWithState(InputControlType.DPadRight, Right, updateTick, deltaTime);
			UpdateWithState(InputControlType.Action1, Jump, updateTick, deltaTime);

			UpdateWithState(InputControlType.Action2, Cast, updateTick, deltaTime);
			UpdateWithState(InputControlType.Action3, Attack, updateTick, deltaTime);
			UpdateWithValue(InputControlType.RightTrigger, Dash ? 1 : 0, updateTick, deltaTime);

			// ResetState();
		}

		private static bool CanDash() =>
            ReflectionHelper.CallMethod<HeroController, bool>(HeroController.instance, "CanDash");

        private static bool CanAttack() =>
            ReflectionHelper.CallMethod<HeroController, bool>(HeroController.instance, "CanAttack");
			
		private static bool CanJump() =>
            ReflectionHelper.CallMethod<HeroController, bool>(HeroController.instance, "CanJump");

        private static bool CanDoubleJump() =>
            ReflectionHelper.CallMethod<HeroController, bool>(HeroController.instance, "CanDoubleJump");

		private static bool CanCast() => 
			ReflectionHelper.CallMethod<HeroController, bool>(HeroController.instance, "CanCast");


		private static bool CanWallJump() =>
            ReflectionHelper.CallMethod<HeroController, bool>(HeroController.instance, "CanWallJump");
		public void ResetState() {
			Up = false;
			Down = false;
			Left = false;
			Right = false;
			Jump = false;
			Attack = false;
			Dash = false;
			Cast = false;
		}
		///<summary>
		///Left = true, right = false
		///</summary>
		public void DoDash(bool direction) {
			if(!CanDash()) return;
			if (direction) {
				HeroController.instance.FaceLeft();
			} else {
				HeroController.instance.FaceRight();
			}

			Dash = true;
		}
		///<summary>
		///Left = true, right = false
		///</summary>
		public void DoWalk(bool direction) {
			if (direction) {
				Left = true;
			} else {
				Right = true;
			}

			// Dash = true;
		}
		public void DoJump() {
			if (!CanJump() && !CanDoubleJump() && !CanWallJump()) return;
			Jump = true;
		}
		///<summary>
		///Left = 0; Right = 1; Up = 2; Down = 3
		///</summary>
		public void DoAttack(int direction) {
			if (!CanAttack()) return;
			if (direction == 0) {
				HeroController.instance.FaceLeft();
			}
			if (direction == 1) {
				HeroController.instance.FaceRight();
			}
			if (direction == 2) {
				Up = true;
			}
			if (direction == 3) {
				Down = true;
			}

			Attack = true;
		}
		///<summary>
		///Left = 0; Right = 1; Up = 2; Down = 3
		///</summary>
		public void DoCast(int direction) {
			if (!CanCast()) return;
			
			if (direction == 0) {
				HeroController.instance.FaceLeft();
			}
			if (direction == 1) {
				HeroController.instance.FaceRight();
			}
			if (direction == 2) {
				Up = true;
			}
			if (direction == 3) {
				Down = true;
			}

			Cast = true;
		}
    }

}