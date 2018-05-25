﻿using System;
using System.Windows.Forms;

namespace OpenBve
{
	/// <summary>The TrainManager is the root class containing functions to load and manage trains within the simulation world.</summary>
	public static partial class TrainManager
	{
		internal enum CarBrakeType
		{
			ElectromagneticStraightAirBrake = 0,
			ElectricCommandBrake = 1,
			AutomaticAirBrake = 2
		}
		internal enum EletropneumaticBrakeType
		{
			None = 0,
			ClosingElectromagneticValve = 1,
			DelayFillingControl = 2
		}

		internal enum AirBrakeType { Main, Auxillary }
		internal struct CarAirBrake
		{
			internal AirBrakeType Type;
			internal bool AirCompressorEnabled;
			internal double AirCompressorMinimumPressure;
			internal double AirCompressorMaximumPressure;
			internal double AirCompressorRate;
			internal double MainReservoirCurrentPressure;
			internal double MainReservoirEqualizingReservoirCoefficient;
			internal double MainReservoirBrakePipeCoefficient;
			internal double EqualizingReservoirCurrentPressure;
			internal double EqualizingReservoirNormalPressure;
			internal double EqualizingReservoirServiceRate;

			internal double EqualizingReservoirEmergencyRate;
			internal double EqualizingReservoirChargeRate;
			internal double BrakePipeCurrentPressure;
			internal double BrakePipeNormalPressure;
			internal double BrakePipeChargeRate;
			internal double BrakePipeServiceRate;
			internal double BrakePipeEmergencyRate;
			internal double AuxillaryReservoirCurrentPressure;
			internal double AuxillaryReservoirMaximumPressure;
			internal double AuxillaryReservoirChargeRate;
			internal double AuxillaryReservoirBrakePipeCoefficient;
			internal double AuxillaryReservoirBrakeCylinderCoefficient;
			internal double BrakeCylinderCurrentPressure;
			internal double BrakeCylinderEmergencyMaximumPressure;
			internal double BrakeCylinderServiceMaximumPressure;
			internal double BrakeCylinderEmergencyChargeRate;
			internal double BrakeCylinderServiceChargeRate;
			internal double BrakeCylinderReleaseRate;
			internal double BrakeCylinderSoundPlayedForPressure;
			internal double StraightAirPipeCurrentPressure;
			internal double StraightAirPipeReleaseRate;
			internal double StraightAirPipeServiceRate;
			internal double StraightAirPipeEmergencyRate;
		}

		
		

		


		// train
		
		// train specs
		
		internal struct TrainAirBrake
		{
			internal AirBrakeHandle Handle;
		}
		internal enum DoorMode
		{
			AutomaticManualOverride = 0,
			Automatic = 1,
			Manual = 2
		}

		

		// trains
		/// <summary>The list of trains available in the simulation.</summary>
		internal static Train[] Trains = new Train[] { };
		/// <summary>A reference to the train of the Trains element that corresponds to the player's train.</summary>
		internal static Train PlayerTrain = null;



		/// <summary>Attempts to load and parse the current train's panel configuration file.</summary>
		/// <param name="TrainPath">The absolute on-disk path to the train folder.</param>
		/// <param name="Encoding">The automatically detected or manually set encoding of the panel configuration file.</param>
		/// <param name="Train">The base train on which to apply the panel configuration.</param>
		internal static void ParsePanelConfig(string TrainPath, System.Text.Encoding Encoding, TrainManager.Train Train)
		{
			string File = OpenBveApi.Path.CombineFile(TrainPath, "panel.animated");
			if (System.IO.File.Exists(File))
			{
				Program.AppendToLogFile("Loading train panel: " + File);
				ObjectManager.AnimatedObjectCollection a = AnimatedObjectParser.ReadObject(File, Encoding, ObjectManager.ObjectLoadMode.DontAllowUnloadOfTextures);
				try
				{
					for (int i = 0; i < a.Objects.Length; i++)
					{
						a.Objects[i].ObjectIndex = ObjectManager.CreateDynamicObject();
					}
					Train.Cars[Train.DriverCar].CarSections[0].Elements = a.Objects;
					Train.Cars[Train.DriverCar].CameraRestrictionMode = World.CameraRestrictionMode.NotAvailable;
					World.CameraRestriction = World.CameraRestrictionMode.NotAvailable;
					World.UpdateViewingDistances();
				}
				catch
				{
					var currentError = Interface.GetInterfaceString("error_critical_file");
					currentError = currentError.Replace("[file]", "panel.animated");
					MessageBox.Show(currentError, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Hand);
					Program.RestartArguments = " ";
					Loading.Cancel = true;
				}
			}
			else
			{
				var Panel2 = false;
				try
				{
					File = OpenBveApi.Path.CombineFile(TrainPath, "panel2.cfg");
					if (System.IO.File.Exists(File))
					{
						Program.AppendToLogFile("Loading train panel: " + File);
						Panel2 = true;
						Panel2CfgParser.ParsePanel2Config("panel2.cfg", TrainPath, Encoding, Train, Train.DriverCar);
						Train.Cars[Train.DriverCar].CameraRestrictionMode = World.CameraRestrictionMode.On;
						World.CameraRestriction = World.CameraRestrictionMode.On;
					}
					else
					{
						File = OpenBveApi.Path.CombineFile(TrainPath, "panel.cfg");
						if (System.IO.File.Exists(File))
						{
							Program.AppendToLogFile("Loading train panel: " + File);
							PanelCfgParser.ParsePanelConfig(TrainPath, Encoding, Train);
							Train.Cars[Train.DriverCar].CameraRestrictionMode = World.CameraRestrictionMode.On;
							World.CameraRestriction = World.CameraRestrictionMode.On;
						}
						else
						{
							World.CameraRestriction = World.CameraRestrictionMode.NotAvailable;
						}
					}
				}
				catch
				{
					var currentError = Interface.GetInterfaceString("errors_critical_file");
					currentError = currentError.Replace("[file]", Panel2 == true ? "panel2.cfg" : "panel.cfg");
					MessageBox.Show(currentError, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Hand);
					Program.RestartArguments = " ";
					Loading.Cancel = true;
				}

			}
		}
		
		// get resistance
		private static double GetResistance(Train Train, int CarIndex, ref Axle Axle, double Speed)
		{
			double t;
			if (CarIndex == 0 & Train.Cars[CarIndex].Specs.CurrentSpeed >= 0.0 || CarIndex == Train.Cars.Length - 1 & Train.Cars[CarIndex].Specs.CurrentSpeed <= 0.0)
			{
				t = Train.Cars[CarIndex].Specs.ExposedFrontalArea;
			}
			else
			{
				t = Train.Cars[CarIndex].Specs.UnexposedFrontalArea;
			}
			double f = t * Train.Cars[CarIndex].Specs.AerodynamicDragCoefficient * Train.Specs.CurrentAirDensity / (2.0 * Train.Cars[CarIndex].Specs.MassCurrent);
			double a = Game.RouteAccelerationDueToGravity * Train.Cars[CarIndex].Specs.CoefficientOfRollingResistance + f * Speed * Speed;
			return a;
		}

		// get critical wheelslip acceleration
		private static double GetCriticalWheelSlipAccelerationForElectricMotor(Train Train, int CarIndex, double AdhesionMultiplier, double UpY, double Speed)
		{
			double NormalForceAcceleration = UpY * Game.RouteAccelerationDueToGravity;
			// TODO: Implement formula that depends on speed here.
			double coefficient = Train.Cars[CarIndex].Specs.CoefficientOfStaticFriction;
			return coefficient * AdhesionMultiplier * NormalForceAcceleration;
		}
		private static double GetCriticalWheelSlipAccelerationForFrictionBrake(Train Train, int CarIndex, double AdhesionMultiplier, double UpY, double Speed)
		{
			double NormalForceAcceleration = UpY * Game.RouteAccelerationDueToGravity;
			// TODO: Implement formula that depends on speed here.
			double coefficient = Train.Cars[CarIndex].Specs.CoefficientOfStaticFriction;
			return coefficient * AdhesionMultiplier * NormalForceAcceleration;
		}

		
		/// <summary>Updates the objects for all trains within the simulation world</summary>
		/// <param name="TimeElapsed">The time elapsed</param>
		/// <param name="ForceUpdate">Whether this is a forced update</param>
		internal static void UpdateTrainObjects(double TimeElapsed, bool ForceUpdate)
		{
			for (int i = 0; i < Trains.Length; i++)
			{
				Trains[i].UpdateObjects(TimeElapsed, ForceUpdate);
			}
		}

		
		
		

		

		/// <summary>This method should be called once a frame to update the position, speed and state of all trains within the simulation</summary>
		/// <param name="TimeElapsed">The time elapsed since the last call to this function</param>
		internal static void UpdateTrains(double TimeElapsed)
		{
			for (int i = 0; i < Trains.Length; i++) {
				Trains[i].Update(TimeElapsed);
			}
			// detect collision
			if (!Game.MinimalisticSimulation & Interface.CurrentOptions.Collisions)
			{
				
				//for (int i = 0; i < Trains.Length; i++) {
				System.Threading.Tasks.Parallel.For(0, Trains.Length, i =>
				{
					// with other trains
					if (Trains[i].State == TrainState.Available)
					{
						double a = Trains[i].Cars[0].FrontAxle.Follower.TrackPosition - Trains[i].Cars[0].FrontAxle.Position +
								   0.5*Trains[i].Cars[0].Length;
						double b = Trains[i].Cars[Trains[i].Cars.Length - 1].RearAxle.Follower.TrackPosition -
								   Trains[i].Cars[Trains[i].Cars.Length - 1].RearAxle.Position - 0.5*Trains[i].Cars[0].Length;
						for (int j = i + 1; j < Trains.Length; j++)
						{
							if (Trains[j].State == TrainState.Available)
							{
								double c = Trains[j].Cars[0].FrontAxle.Follower.TrackPosition -
										   Trains[j].Cars[0].FrontAxle.Position + 0.5*Trains[j].Cars[0].Length;
								double d = Trains[j].Cars[Trains[j].Cars.Length - 1].RearAxle.Follower.TrackPosition -
										   Trains[j].Cars[Trains[j].Cars.Length - 1].RearAxle.Position -
										   0.5*Trains[j].Cars[0].Length;
								if (a > d & b < c)
								{
									if (a > c)
									{
										// i > j
										int k = Trains[i].Cars.Length - 1;
										if (Trains[i].Cars[k].Specs.CurrentSpeed < Trains[j].Cars[0].Specs.CurrentSpeed)
										{
											double v = Trains[j].Cars[0].Specs.CurrentSpeed -
													   Trains[i].Cars[k].Specs.CurrentSpeed;
											double s = (Trains[i].Cars[k].Specs.CurrentSpeed*Trains[i].Cars[k].Specs.MassCurrent +
														Trains[j].Cars[0].Specs.CurrentSpeed*Trains[j].Cars[0].Specs.MassCurrent)/
													   (Trains[i].Cars[k].Specs.MassCurrent + Trains[j].Cars[0].Specs.MassCurrent);
											Trains[i].Cars[k].Specs.CurrentSpeed = s;
											Trains[j].Cars[0].Specs.CurrentSpeed = s;
											double e = 0.5*(c - b) + 0.0001;
											Trains[i].Cars[k].FrontAxle.Follower.Update(Trains[i].Cars[k].FrontAxle.Follower.TrackPosition + e, false, false);
											Trains[i].Cars[k].RearAxle.Follower.Update(Trains[i].Cars[k].RearAxle.Follower.TrackPosition + e, false, false);
											Trains[j].Cars[0].FrontAxle.Follower.Update(Trains[j].Cars[0].FrontAxle.Follower.TrackPosition - e, false, false);

											Trains[j].Cars[0].RearAxle.Follower.Update(Trains[j].Cars[0].RearAxle.Follower.TrackPosition - e, false, false);
											if (Interface.CurrentOptions.Derailments)
											{
												double f = 2.0/
														   (Trains[i].Cars[k].Specs.MassCurrent +
															Trains[j].Cars[0].Specs.MassCurrent);
												double fi = Trains[j].Cars[0].Specs.MassCurrent*f;
												double fj = Trains[i].Cars[k].Specs.MassCurrent*f;
												double vi = v*fi;
												double vj = v*fj;
												if (vi > Game.CriticalCollisionSpeedDifference)
													Trains[i].Derail(k, TimeElapsed);
												if (vj > Game.CriticalCollisionSpeedDifference)
													Trains[j].Derail(i, TimeElapsed);
											}
											// adjust cars for train i
											for (int h = Trains[i].Cars.Length - 2; h >= 0; h--)
											{
												a = Trains[i].Cars[h + 1].FrontAxle.Follower.TrackPosition -
													Trains[i].Cars[h + 1].FrontAxle.Position + 0.5*Trains[i].Cars[h + 1].Length;
												b = Trains[i].Cars[h].RearAxle.Follower.TrackPosition -
													Trains[i].Cars[h].RearAxle.Position - 0.5*Trains[i].Cars[h].Length;
												d = b - a - Trains[i].Couplers[h].MinimumDistanceBetweenCars;
												if (d < 0.0)
												{
													d -= 0.0001;
													Trains[i].Cars[h].FrontAxle.Follower.Update(Trains[i].Cars[h].FrontAxle.Follower.TrackPosition - d, false, false);
													Trains[i].Cars[h].RearAxle.Follower.Update(Trains[i].Cars[h].RearAxle.Follower.TrackPosition - d, false, false);
													if (Interface.CurrentOptions.Derailments)
													{
														double f = 2.0/
																   (Trains[i].Cars[h + 1].Specs.MassCurrent +
																	Trains[i].Cars[h].Specs.MassCurrent);
														double fi = Trains[i].Cars[h + 1].Specs.MassCurrent*f;
														double fj = Trains[i].Cars[h].Specs.MassCurrent*f;
														double vi = v*fi;
														double vj = v*fj;
														if (vi > Game.CriticalCollisionSpeedDifference)
															Trains[i].Derail(h + 1, TimeElapsed);
														if (vj > Game.CriticalCollisionSpeedDifference)
															Trains[i].Derail(h, TimeElapsed);
													}
													Trains[i].Cars[h].Specs.CurrentSpeed =
														Trains[i].Cars[h + 1].Specs.CurrentSpeed;
												}
											}
											// adjust cars for train j
											for (int h = 1; h < Trains[j].Cars.Length; h++)
											{
												a = Trains[j].Cars[h - 1].RearAxle.Follower.TrackPosition -
													Trains[j].Cars[h - 1].RearAxle.Position - 0.5*Trains[j].Cars[h - 1].Length;
												b = Trains[j].Cars[h].FrontAxle.Follower.TrackPosition -
													Trains[j].Cars[h].FrontAxle.Position + 0.5*Trains[j].Cars[h].Length;
												d = a - b - Trains[j].Couplers[h - 1].MinimumDistanceBetweenCars;
												if (d < 0.0)
												{
													d -= 0.0001;
													Trains[j].Cars[h].FrontAxle.Follower.Update(Trains[j].Cars[h].FrontAxle.Follower.TrackPosition + d, false, false);
													Trains[j].Cars[h].RearAxle.Follower.Update(Trains[j].Cars[h].RearAxle.Follower.TrackPosition + d, false, false);
													if (Interface.CurrentOptions.Derailments)
													{
														double f = 2.0/
																   (Trains[j].Cars[h - 1].Specs.MassCurrent +
																	Trains[j].Cars[h].Specs.MassCurrent);
														double fi = Trains[j].Cars[h - 1].Specs.MassCurrent*f;
														double fj = Trains[j].Cars[h].Specs.MassCurrent*f;
														double vi = v*fi;
														double vj = v*fj;
														if (vi > Game.CriticalCollisionSpeedDifference)
															Trains[j].Derail(h -1, TimeElapsed);
														if (vj > Game.CriticalCollisionSpeedDifference)
															Trains[j].Derail(h, TimeElapsed);
													}
													Trains[j].Cars[h].Specs.CurrentSpeed =
														Trains[j].Cars[h - 1].Specs.CurrentSpeed;
												}
											}
										}
									}
									else
									{
										// i < j
										int k = Trains[j].Cars.Length - 1;
										if (Trains[i].Cars[0].Specs.CurrentSpeed > Trains[j].Cars[k].Specs.CurrentSpeed)
										{
											double v = Trains[i].Cars[0].Specs.CurrentSpeed -
													   Trains[j].Cars[k].Specs.CurrentSpeed;
											double s = (Trains[i].Cars[0].Specs.CurrentSpeed*Trains[i].Cars[0].Specs.MassCurrent +
														Trains[j].Cars[k].Specs.CurrentSpeed*Trains[j].Cars[k].Specs.MassCurrent)/
													   (Trains[i].Cars[0].Specs.MassCurrent + Trains[j].Cars[k].Specs.MassCurrent);
											Trains[i].Cars[0].Specs.CurrentSpeed = s;
											Trains[j].Cars[k].Specs.CurrentSpeed = s;
											double e = 0.5*(a - d) + 0.0001;
											Trains[i].Cars[0].FrontAxle.Follower.Update(Trains[i].Cars[0].FrontAxle.Follower.TrackPosition - e, false, false);
											Trains[i].Cars[0].RearAxle.Follower.Update(Trains[i].Cars[0].RearAxle.Follower.TrackPosition - e, false, false);
											Trains[j].Cars[k].FrontAxle.Follower.Update(Trains[j].Cars[k].FrontAxle.Follower.TrackPosition + e, false, false);
											Trains[j].Cars[k].RearAxle.Follower.Update(Trains[j].Cars[k].RearAxle.Follower.TrackPosition + e, false, false);
											if (Interface.CurrentOptions.Derailments)
											{
												double f = 2.0/
														   (Trains[i].Cars[0].Specs.MassCurrent +
															Trains[j].Cars[k].Specs.MassCurrent);
												double fi = Trains[j].Cars[k].Specs.MassCurrent*f;
												double fj = Trains[i].Cars[0].Specs.MassCurrent*f;
												double vi = v*fi;
												double vj = v*fj;
												if (vi > Game.CriticalCollisionSpeedDifference)
													Trains[i].Derail(0, TimeElapsed);
												if (vj > Game.CriticalCollisionSpeedDifference)
													Trains[j].Derail(k, TimeElapsed);
											}
											// adjust cars for train i
											for (int h = 1; h < Trains[i].Cars.Length; h++)
											{
												a = Trains[i].Cars[h - 1].RearAxle.Follower.TrackPosition -
													Trains[i].Cars[h - 1].RearAxle.Position - 0.5*Trains[i].Cars[h - 1].Length;
												b = Trains[i].Cars[h].FrontAxle.Follower.TrackPosition -
													Trains[i].Cars[h].FrontAxle.Position + 0.5*Trains[i].Cars[h].Length;
												d = a - b - Trains[i].Couplers[h - 1].MinimumDistanceBetweenCars;
												if (d < 0.0)
												{
													d -= 0.0001;
													Trains[i].Cars[h].FrontAxle.Follower.Update(Trains[i].Cars[h].FrontAxle.Follower.TrackPosition + d, false, false);
													Trains[i].Cars[h].RearAxle.Follower.Update(Trains[i].Cars[h].RearAxle.Follower.TrackPosition + d, false, false);
													if (Interface.CurrentOptions.Derailments)
													{
														double f = 2.0/
																   (Trains[i].Cars[h - 1].Specs.MassCurrent +
																	Trains[i].Cars[h].Specs.MassCurrent);
														double fi = Trains[i].Cars[h - 1].Specs.MassCurrent*f;
														double fj = Trains[i].Cars[h].Specs.MassCurrent*f;
														double vi = v*fi;
														double vj = v*fj;
														if (vi > Game.CriticalCollisionSpeedDifference)
															Trains[i].Derail(h -1, TimeElapsed);
														if (vj > Game.CriticalCollisionSpeedDifference)
															Trains[i].Derail(h, TimeElapsed);
													}
													Trains[i].Cars[h].Specs.CurrentSpeed =
														Trains[i].Cars[h - 1].Specs.CurrentSpeed;
												}
											}
											// adjust cars for train j
											for (int h = Trains[j].Cars.Length - 2; h >= 0; h--)
											{
												a = Trains[j].Cars[h + 1].FrontAxle.Follower.TrackPosition -
													Trains[j].Cars[h + 1].FrontAxle.Position + 0.5*Trains[j].Cars[h + 1].Length;
												b = Trains[j].Cars[h].RearAxle.Follower.TrackPosition -
													Trains[j].Cars[h].RearAxle.Position - 0.5*Trains[j].Cars[h].Length;
												d = b - a - Trains[j].Couplers[h].MinimumDistanceBetweenCars;
												if (d < 0.0)
												{
													d -= 0.0001;
													Trains[j].Cars[h].FrontAxle.Follower.Update(Trains[j].Cars[h].FrontAxle.Follower.TrackPosition - d, false, false);
													Trains[j].Cars[h].RearAxle.Follower.Update(Trains[j].Cars[h].RearAxle.Follower.TrackPosition - d, false, false);
													if (Interface.CurrentOptions.Derailments)
													{
														double f = 2.0/
																   (Trains[j].Cars[h + 1].Specs.MassCurrent +
																	Trains[j].Cars[h].Specs.MassCurrent);
														double fi = Trains[j].Cars[h + 1].Specs.MassCurrent*f;
														double fj = Trains[j].Cars[h].Specs.MassCurrent*f;
														double vi = v*fi;
														double vj = v*fj;
														if (vi > Game.CriticalCollisionSpeedDifference)
															Trains[j].Derail(h + 1, TimeElapsed);
														if (vj > Game.CriticalCollisionSpeedDifference)
															Trains[j].Derail(h, TimeElapsed);
													}
													Trains[j].Cars[h].Specs.CurrentSpeed =
														Trains[j].Cars[h + 1].Specs.CurrentSpeed;
												}
											}
										}
									}
								}
							}

						}
					}
					// with buffers
					if (i == PlayerTrain.TrainIndex)
					{
						double a = Trains[i].Cars[0].FrontAxle.Follower.TrackPosition - Trains[i].Cars[0].FrontAxle.Position +
								   0.5*Trains[i].Cars[0].Length;
						double b = Trains[i].Cars[Trains[i].Cars.Length - 1].RearAxle.Follower.TrackPosition -
								   Trains[i].Cars[Trains[i].Cars.Length - 1].RearAxle.Position - 0.5*Trains[i].Cars[0].Length;
						for (int j = 0; j < Game.BufferTrackPositions.Length; j++)
						{
							if (a > Game.BufferTrackPositions[j] & b < Game.BufferTrackPositions[j])
							{
								a += 0.0001;
								b -= 0.0001;
								double da = a - Game.BufferTrackPositions[j];
								double db = Game.BufferTrackPositions[j] - b;
								if (da < db)
								{
									// front
									Trains[i].Cars[0].UpdateTrackFollowers(-da, false, false);
									if (Interface.CurrentOptions.Derailments &&
										Math.Abs(Trains[i].Cars[0].Specs.CurrentSpeed) > Game.CriticalCollisionSpeedDifference)
									{
										Trains[i].Derail(0, TimeElapsed);
									}
									Trains[i].Cars[0].Specs.CurrentSpeed = 0.0;
									for (int h = 1; h < Trains[i].Cars.Length; h++)
									{
										a = Trains[i].Cars[h - 1].RearAxle.Follower.TrackPosition -
											Trains[i].Cars[h - 1].RearAxle.Position - 0.5*Trains[i].Cars[h - 1].Length;
										b = Trains[i].Cars[h].FrontAxle.Follower.TrackPosition -
											Trains[i].Cars[h].FrontAxle.Position + 0.5*Trains[i].Cars[h].Length;
										double d = a - b - Trains[i].Couplers[h - 1].MinimumDistanceBetweenCars;
										if (d < 0.0)
										{
											d -= 0.0001;
											Trains[i].Cars[h].UpdateTrackFollowers(d, false, false);
											if (Interface.CurrentOptions.Derailments &&
												Math.Abs(Trains[i].Cars[h].Specs.CurrentSpeed) >
												Game.CriticalCollisionSpeedDifference)
											{
												Trains[i].Derail(h, TimeElapsed);
											}
											Trains[i].Cars[h].Specs.CurrentSpeed = 0.0;
										}
									}
								}
								else
								{
									// rear
									int c = Trains[i].Cars.Length - 1;
									Trains[i].Cars[c].UpdateTrackFollowers(db, false, false);
									if (Interface.CurrentOptions.Derailments &&
										Math.Abs(Trains[i].Cars[c].Specs.CurrentSpeed) > Game.CriticalCollisionSpeedDifference)
									{
										Trains[i].Derail(c, TimeElapsed);
									}
									Trains[i].Cars[c].Specs.CurrentSpeed = 0.0;
									for (int h = Trains[i].Cars.Length - 2; h >= 0; h--)
									{
										a = Trains[i].Cars[h + 1].FrontAxle.Follower.TrackPosition -
											Trains[i].Cars[h + 1].FrontAxle.Position + 0.5*Trains[i].Cars[h + 1].Length;
										b = Trains[i].Cars[h].RearAxle.Follower.TrackPosition -
											Trains[i].Cars[h].RearAxle.Position - 0.5*Trains[i].Cars[h].Length;
										double d = b - a - Trains[i].Couplers[h].MinimumDistanceBetweenCars;
										if (d < 0.0)
										{
											d -= 0.0001;
											Trains[i].Cars[h].UpdateTrackFollowers(-d, false, false);
											if (Interface.CurrentOptions.Derailments &&
												Math.Abs(Trains[i].Cars[h].Specs.CurrentSpeed) >
												Game.CriticalCollisionSpeedDifference)
											{
												Trains[i].Derail(h, TimeElapsed);
											}
											Trains[i].Cars[h].Specs.CurrentSpeed = 0.0;
										}
									}
								}
							}
						}
					}
				});
			}
			// compute final angles and positions
			//for (int i = 0; i < Trains.Length; i++) {
			System.Threading.Tasks.Parallel.For(0, Trains.Length, i =>
			{
				if (Trains[i].State != TrainState.Disposed & Trains[i].State != TrainManager.TrainState.Bogus)
				{
					for (int j = 0; j < Trains[i].Cars.Length; j++)
					{
						Trains[i].Cars[j].FrontAxle.Follower.UpdateWorldCoordinates(true);
						Trains[i].Cars[j].FrontBogie.FrontAxle.Follower.UpdateWorldCoordinates(true);
						Trains[i].Cars[j].FrontBogie.RearAxle.Follower.UpdateWorldCoordinates(true);
						Trains[i].Cars[j].RearAxle.Follower.UpdateWorldCoordinates(true);
						Trains[i].Cars[j].RearBogie.FrontAxle.Follower.UpdateWorldCoordinates(true);
						Trains[i].Cars[j].RearBogie.RearAxle.Follower.UpdateWorldCoordinates(true);
						if (TimeElapsed == 0.0 | TimeElapsed > 0.5)
						{
							//Don't update the toppling etc. with excessive or no time
							continue;
						}
						Trains[i].Cars[j].UpdateTopplingCantAndSpring(TimeElapsed);
						Trains[i].Cars[j].FrontBogie.UpdateTopplingCantAndSpring();
						Trains[i].Cars[j].RearBogie.UpdateTopplingCantAndSpring();
					}
				}
			});
		}
		

		// apply notch
		internal static void ApplyNotch(Train Train, int PowerValue, bool PowerRelative, int BrakeValue, bool BrakeRelative)
		{
			// determine notch
			int p = PowerRelative ? PowerValue + Train.Handles.Power.Driver : PowerValue;
			if (p < 0)
			{
				p = 0;
			}
			else if (p > Train.Specs.MaximumPowerNotch)
			{
				p = Train.Specs.MaximumPowerNotch;
			}
			int b = BrakeRelative ? BrakeValue + Train.Handles.Brake.Driver : BrakeValue;
			if (b < 0)
			{
				b = 0;
			}
			else if (b > Train.Specs.MaximumBrakeNotch)
			{
				b = Train.Specs.MaximumBrakeNotch;
			}
			// power sound
			if (p < Train.Handles.Power.Driver)
			{
				if (p > 0)
				{
					// down (not min)
					Sounds.SoundBuffer buffer = Train.Cars[Train.DriverCar].Sounds.MasterControllerDown.Buffer;
					if (buffer != null)
					{
						OpenBveApi.Math.Vector3 pos = Train.Cars[Train.DriverCar].Sounds.MasterControllerDown.Position;
						Sounds.PlaySound(buffer, 1.0, 1.0, pos, Train, Train.DriverCar, false);
					}
				}
				else
				{
					// min
					Sounds.SoundBuffer buffer = Train.Cars[Train.DriverCar].Sounds.MasterControllerMin.Buffer;
					if (buffer != null)
					{
						OpenBveApi.Math.Vector3 pos = Train.Cars[Train.DriverCar].Sounds.MasterControllerMin.Position;
						Sounds.PlaySound(buffer, 1.0, 1.0, pos, Train, Train.DriverCar, false);
					}
				}
			}
			else if (p > Train.Handles.Power.Driver)
			{
				if (p < Train.Specs.MaximumPowerNotch)
				{
					// up (not max)
					Sounds.SoundBuffer buffer = Train.Cars[Train.DriverCar].Sounds.MasterControllerUp.Buffer;
					if (buffer != null)
					{
						OpenBveApi.Math.Vector3 pos = Train.Cars[Train.DriverCar].Sounds.MasterControllerUp.Position;
						Sounds.PlaySound(buffer, 1.0, 1.0, pos, Train, Train.DriverCar, false);
					}
				}
				else
				{
					// max
					Sounds.SoundBuffer buffer = Train.Cars[Train.DriverCar].Sounds.MasterControllerMax.Buffer;
					if (buffer != null)
					{
						OpenBveApi.Math.Vector3 pos = Train.Cars[Train.DriverCar].Sounds.MasterControllerMax.Position;
						Sounds.PlaySound(buffer, 1.0, 1.0, pos, Train, Train.DriverCar, false);
					}
				}
			}
			// brake sound
			if (b < Train.Handles.Brake.Driver)
			{
				// brake release
				Sounds.SoundBuffer buffer = Train.Cars[Train.DriverCar].Sounds.Brake.Buffer;
				if (buffer != null)
				{
					OpenBveApi.Math.Vector3 pos = Train.Cars[Train.DriverCar].Sounds.Brake.Position;
					Sounds.PlaySound(buffer, 1.0, 1.0, pos, Train, Train.DriverCar, false);
				}
				if (b > 0)
				{
					// brake release (not min)
					buffer = Train.Cars[Train.DriverCar].Sounds.BrakeHandleRelease.Buffer;
					if (buffer != null)
					{
						OpenBveApi.Math.Vector3 pos = Train.Cars[Train.DriverCar].Sounds.BrakeHandleRelease.Position;
						Sounds.PlaySound(buffer, 1.0, 1.0, pos, Train, Train.DriverCar, false);
					}
				}
				else
				{
					// brake min
					buffer = Train.Cars[Train.DriverCar].Sounds.BrakeHandleMin.Buffer;
					if (buffer != null)
					{
						OpenBveApi.Math.Vector3 pos = Train.Cars[Train.DriverCar].Sounds.BrakeHandleMin.Position;
						Sounds.PlaySound(buffer, 1.0, 1.0, pos, Train, Train.DriverCar, false);
					}
				}
			}
			else if (b > Train.Handles.Brake.Driver)
			{
				// brake
				Sounds.SoundBuffer buffer = Train.Cars[Train.DriverCar].Sounds.BrakeHandleApply.Buffer;
				if (buffer != null)
				{
					OpenBveApi.Math.Vector3 pos = Train.Cars[Train.DriverCar].Sounds.BrakeHandleApply.Position;
					Sounds.PlaySound(buffer, 1.0, 1.0, pos, Train, Train.DriverCar, false);
				}
			}
			// apply notch
			if (Train.Specs.SingleHandle)
			{
				if (b != 0) p = 0;
			}
			Train.Handles.Power.Driver = p;
			Train.Handles.Brake.Driver = b;
			Game.AddBlackBoxEntry(Game.BlackBoxEventToken.None);
			// plugin
			if (Train.Plugin != null)
			{
				Train.Plugin.UpdatePower();
				Train.Plugin.UpdateBrake();
			}
		}	

		

		// update speeds
		

		/// <summary>Gets the delay value for the current power notch</summary>
		/// <param name="Notch">The current notch</param>
		/// <param name="DelayValues">The array of delay values</param>
		/// <returns>The delay value to apply</returns>
		private static double GetDelay(int Notch, double[] DelayValues)
		{
			if (DelayValues == null || DelayValues.Length == 0)
			{
				return 0.0;
			}
			if (Notch < DelayValues.Length)
			{
				return DelayValues[Notch];
			}
			return DelayValues[DelayValues.Length - 1];
		}

		// update train physics and controls
		

	}
}