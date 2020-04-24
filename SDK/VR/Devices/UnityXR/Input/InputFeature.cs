using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace Liminal.SDK.XR
{
	public enum EPressState
	{
		None,
		Down,
		Pressing,
		Up
	}

	public abstract class InputFeature
	{
		#region Fields
		#endregion

		#region Properties
		protected InputDevice? Device { get; private set; }
		public EPressState PressState { get; protected set; }
		public abstract string Name { get; }
		public bool IsUpdated { get; private set; } 
		#endregion

		public InputFeature()
		{
			PressState = EPressState.None;
			MarkUpdated();
		}

		public void AssignDevice(InputDevice aDevice)
		{
			if (Device.HasValue) return;

			Device = aDevice;
		}

		public virtual void UpdateState()
		{
			MarkUpdated();
		}

		public void MarkUpdated() { IsUpdated = true; }
		public void Clean() { IsUpdated = false; }
	}

	public abstract class InputFeature<T> : InputFeature where T : IEquatable<T>
	{
		// RawValue is assigned, also assign the 'normalised' Value
		public virtual T RawValue
		{
			get; protected set;
		}
		public T Value { get; protected set; }

		public InputFeatureUsage<T> BaseFeature { get; }

		public override string Name => BaseFeature.name;

		public InputFeature(InputFeatureUsage<T> aBaseFeature) : base()
		{
			BaseFeature = aBaseFeature;
		}
	}

	public class ButtonInputFeature : InputFeature<bool>
	{
		public override bool RawValue
		{
			get => base.RawValue;
			protected set
			{
				base.RawValue = value;
				Value = RawValue;
			}
		}

		public ButtonInputFeature(InputFeatureUsage<bool> aBaseFeature) : base(aBaseFeature)
		{
		}

		public override void UpdateState()
		{
			base.UpdateState();

			if (!Device.HasValue) return;

			if (!Device.Value.TryGetFeatureValue(BaseFeature, out bool isPressed))
			{
				// couldn't get input for the feature, so mark press state as none
				PressState = EPressState.None;
				RawValue = false;
			}

			// received a value, so update accordingly
			EPressState currentState = PressState;
			RawValue = isPressed;

			if (isPressed)
			{
				switch (currentState)
				{
					case EPressState.None:
						PressState = EPressState.Down;
						break;
					case EPressState.Down:
						PressState = EPressState.Pressing;
						break;
					default:
						break;
				}
			}
			else
			{
				switch (currentState)
				{
					case EPressState.Pressing:
						PressState = EPressState.Up;
						break;
					case EPressState.Up:
						PressState = EPressState.None;
						break;
					default:
						break;
				}
			}
		}
	}

	public interface AxisInputFeature { }

	public class Axis1DInputFeature : InputFeature<float>, AxisInputFeature
	{
		private const float THRESHOLD = 0.1f;

		public override float RawValue
		{
			get => base.RawValue;
			protected set
			{
				base.RawValue = value;
				Value = value >= THRESHOLD ? 1f : 0f;
			}
		}

		public Axis1DInputFeature(InputFeatureUsage<float> aBaseFeature) : base(aBaseFeature)
		{
		}

		public override void UpdateState()
		{
			base.UpdateState();

			if (!Device.HasValue) return;

			if (!Device.Value.TryGetFeatureValue(BaseFeature, out float rawActuated))
			{
				// couldn't get input for the feature, so mark press state as none
				PressState = EPressState.None;
				RawValue = 0.0f;
			}

			// received a value, so update accordingly
			EPressState currentState = PressState;
			RawValue = rawActuated;

			// if above or equal to the threshold the axis is considered 'pressed'
			if (rawActuated >= THRESHOLD)
			{
				switch (currentState)
				{
					case EPressState.None:
						PressState = EPressState.Down;
						break;
					case EPressState.Down:
						PressState = EPressState.Pressing;
						break;
					default:
						break;
				}
			}
			else
			{
				switch (currentState)
				{
					case EPressState.Pressing:
						PressState = EPressState.Up;
						break;
					case EPressState.Up:
						PressState = EPressState.None;
						break;
					default:
						break;
				}
			}
		}
	}

	public class Axis2DInputFeature : InputFeature<Vector2>, AxisInputFeature
	{
		private const float THRESHOLD = 0.1f;

		public override Vector2 RawValue
		{
			get => base.RawValue;
			protected set
			{
				base.RawValue = value;

				Value = new Vector2(
					Mathf.Abs(base.RawValue.x) >= THRESHOLD ? 1f * Mathf.Sign(base.RawValue.x) : 0f,
					Mathf.Abs(base.RawValue.y) >= THRESHOLD ? 1f * Mathf.Sign(base.RawValue.y) : 0f
				);
			}
		}

		public Axis2DInputFeature(InputFeatureUsage<Vector2> aBaseFeature) : base(aBaseFeature)
		{
		}

		public override void UpdateState()
		{
			base.UpdateState();

			if (!Device.HasValue) return;

			if (!Device.Value.TryGetFeatureValue(BaseFeature, out Vector2 rawActuated))
			{
				// couldn't get input for the feature, so mark press state as none
				PressState = EPressState.None;
				RawValue = Vector2.zero;
			}

			// received a value, so update accordingly
			EPressState currentState = PressState;
			RawValue = rawActuated;

			// if either axis exceeds the threshold, considered pressed
			if (Mathf.Abs(rawActuated.x) >= THRESHOLD ||
				Mathf.Abs(rawActuated.y) >= THRESHOLD)
			{
				switch (currentState)
				{
					case EPressState.None:
						PressState = EPressState.Down;
						break;
					case EPressState.Down:
						PressState = EPressState.Pressing;
						break;
					default:
						break;
				}
			}
			else
			{
				switch (currentState)
				{
					case EPressState.Pressing:
						PressState = EPressState.Up;
						break;
					case EPressState.Up:
						PressState = EPressState.None;
						break;
					default:
						break;
				}
			}
		}
	}
}