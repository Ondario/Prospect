using System.Collections.Generic;
using Newtonsoft.Json;

namespace Prospect.Unreal.Assets.DataTables
{
    public class PlayerTuningCollection : Dictionary<string, PlayerTuning>
    {
    }

    public class PlayerTuning
    {
        [JsonProperty("WalkSpeed")]
        public float WalkSpeed { get; set; } = 357.0f;

        [JsonProperty("SprintSpeed")]
        public float SprintSpeed { get; set; }

        [JsonProperty("CrouchSpeed")]
        public float CrouchSpeed { get; set; } = 150.0f;

        [JsonProperty("SprintSpeedMultiplier")]
        public float SprintSpeedMultiplier { get; set; } = 1.91f;

        [JsonProperty("JumpZVelocity")]
        public float JumpZVelocity { get; set; }

        [JsonProperty("MaxAcceleration")]
        public float MaxAcceleration { get; set; }

        [JsonProperty("BrakingDeceleration")]
        public float BrakingDeceleration { get; set; }

        [JsonProperty("GroundFriction")]
        public float GroundFriction { get; set; }

        [JsonProperty("MaxWalkSpeed")]
        public float MaxWalkSpeed { get; set; }

        [JsonProperty("MaxWalkSpeedCrouched")]
        public float MaxWalkSpeedCrouched { get; set; }

        [JsonProperty("MaxFlySpeed")]
        public float MaxFlySpeed { get; set; }

        [JsonProperty("MaxSwimSpeed")]
        public float MaxSwimSpeed { get; set; }

        [JsonProperty("AirControl")]
        public float AirControl { get; set; }

        [JsonProperty("AirControlBoostMultiplier")]
        public float AirControlBoostMultiplier { get; set; }

        [JsonProperty("FallingLateralFriction")]
        public float FallingLateralFriction { get; set; }

        [JsonProperty("GravityScale")]
        public float GravityScale { get; set; } = 1.0f;

        [JsonProperty("MaxStepHeight")]
        public float MaxStepHeight { get; set; }

        [JsonProperty("MinAnalogWalkSpeed")]
        public float MinAnalogWalkSpeed { get; set; }

        [JsonProperty("RotationRate")]
        public RotationRate RotationRate { get; set; }

        [JsonProperty("UseControllerDesiredRotation")]
        public bool UseControllerDesiredRotation { get; set; }

        [JsonProperty("OrientRotationToMovement")]
        public bool OrientRotationToMovement { get; set; }

        [JsonProperty("bUseSeparateBrakingFriction")]
        public bool UseSeparateBrakingFriction { get; set; }

        [JsonProperty("BrakingFriction")]
        public float BrakingFriction { get; set; }

        [JsonProperty("BrakingSubStepTime")]
        public float BrakingSubStepTime { get; set; }

        [JsonProperty("MaxSimulationTimeStep")]
        public float MaxSimulationTimeStep { get; set; }

        [JsonProperty("MaxSimulationIterations")]
        public int MaxSimulationIterations { get; set; }

        // Helper methods for movement calculations
        public float GetEffectiveWalkSpeed(bool isSprinting = false, bool isCrouching = false)
        {
            if (isCrouching)
                return CrouchSpeed;
            if (isSprinting)
                return WalkSpeed * SprintSpeedMultiplier;
            return WalkSpeed;
        }

        public float GetMaxSpeedForMode(MovementMode mode)
        {
            return mode switch
            {
                MovementMode.Walking => MaxWalkSpeed,
                MovementMode.Crouching => MaxWalkSpeedCrouched,
                MovementMode.Flying => MaxFlySpeed,
                MovementMode.Swimming => MaxSwimSpeed,
                _ => MaxWalkSpeed
            };
        }
    }

    public class RotationRate
    {
        [JsonProperty("Pitch")]
        public float Pitch { get; set; }

        [JsonProperty("Yaw")]
        public float Yaw { get; set; }

        [JsonProperty("Roll")]
        public float Roll { get; set; }
    }

    public enum MovementMode
    {
        Walking,
        Crouching,
        Flying,
        Swimming,
        Falling
    }
} 