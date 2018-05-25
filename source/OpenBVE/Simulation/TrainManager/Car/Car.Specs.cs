﻿namespace OpenBve
{
	/// <summary>The TrainManager is the root class containing functions to load and manage trains within the simulation world.</summary>
	public static partial class TrainManager
	{
		internal struct CarSpecs
		{
			internal bool IsDriverCar;
			/// motor
			internal bool IsMotorCar;
			internal AccelerationCurve[] AccelerationCurves;
			internal AccelerationCurve[] DecelerationCurves;

			internal double BrakeDecelerationAtServiceMaximumPressure(int Notch)
			{
				if (Notch == 0)
				{
					return this.DecelerationCurves[0].GetAccelerationOutput(this.CurrentSpeed, 1.0);
				}
				if (this.DecelerationCurves.Length >= Notch)
				{
					return this.DecelerationCurves[Notch - 1].GetAccelerationOutput(this.CurrentSpeed, 1.0);
				}
				return this.DecelerationCurves[this.DecelerationCurves.Length - 1].GetAccelerationOutput(this.CurrentSpeed, 1.0);
			}

			internal double AccelerationCurveMaximum;
			internal double JerkPowerUp;
			internal double JerkPowerDown;
			internal double JerkBrakeUp;
			internal double JerkBrakeDown;
			/// brake
			internal double BrakeControlSpeed;
			internal double MotorDeceleration;
			/// physical properties
			internal double MassEmpty;
			internal double MassCurrent;
			internal double ExposedFrontalArea;
			internal double UnexposedFrontalArea;
			internal double CoefficientOfStaticFriction;
			internal double CoefficientOfRollingResistance;
			internal double AerodynamicDragCoefficient;
			internal double CenterOfGravityHeight;
			internal double CriticalTopplingAngle;
			/// current data
			internal double CurrentSpeed;
			internal double CurrentPerceivedSpeed;
			internal double CurrentAcceleration;
			/// <summary>The acceleration generated by the motor. Is positive for power and negative for brake, regardless of the train's direction.</summary>
			internal double CurrentAccelerationOutput;
			internal double CurrentRollDueToTopplingAngle;
			internal double CurrentRollDueToCantAngle;
			internal double CurrentRollDueToShakingAngle;
			internal double CurrentRollDueToShakingAngularSpeed;
			internal double CurrentRollShakeDirection;
			internal double CurrentPitchDueToAccelerationAngle;
			internal double CurrentPitchDueToAccelerationAngularSpeed;
			internal double CurrentPitchDueToAccelerationTargetAngle;
			internal double CurrentPitchDueToAccelerationFastValue;
			internal double CurrentPitchDueToAccelerationMediumValue;
			internal double CurrentPitchDueToAccelerationSlowValue;
			/// systems
			internal CarHoldBrake HoldBrake;
			internal CarConstSpeed ConstSpeed;
			internal CarReAdhesionDevice ReAdhesionDevice;
			internal CarBrakeType BrakeType;
			internal EletropneumaticBrakeType ElectropneumaticType;
			internal CarAirBrake AirBrake;
			/// doors
			
			internal double DoorOpenFrequency;
			internal double DoorCloseFrequency;
			internal double DoorOpenPitch;
			internal double DoorClosePitch;
		}
	}
}
