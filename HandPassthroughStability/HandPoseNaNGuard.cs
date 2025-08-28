// Assets/HandPassthroughStability/Runtime/Input/HandPoseNaNGuard.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.SubsystemsImplementation;
using UnityEngine.XR;

namespace HPS.Stability.Input
{
    public class HandPoseNaNGuard : MonoBehaviour
    {
        XRHandSubsystem _hands;

        static readonly XRHandJointID[] _checkJoints =
        {
            XRHandJointID.Wrist, XRHandJointID.IndexTip, XRHandJointID.MiddleTip,
            XRHandJointID.RingTip, XRHandJointID.LittleTip, XRHandJointID.ThumbTip
        };

        void OnEnable()
        {
            _hands = GetHandSubsystem();
            if (_hands == null)
                Debug.LogWarning("[NaNGuard] XRHandSubsystem not found.");
        }

        void Update()
        {
            if (_hands == null || !_hands.running) return;

            CheckHand(_hands.leftHand, "Left");
            CheckHand(_hands.rightHand, "Right");
        }

        void CheckHand(XRHand hand, string label)
        {
            if (!hand.isTracked) return;

            foreach (var id in _checkJoints)
            {
                var joint = hand.GetJoint(id);
                if (!joint.TryGetPose(out var pose)) continue;

                var p = pose.position;
                if (float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsNaN(p.z))
                {
                    var msg = $"[HAND_NaN] {label} {id} pos=({p.x},{p.y},{p.z})";
                    Debug.LogWarning(msg);
                    HPS.Stability.Diagnostics.LogFile.WriteLine(msg);
                }
            }
        }

        static XRHandSubsystem GetHandSubsystem()
        {
            var subsystems = new List<XRHandSubsystem>();
            SubsystemManager.GetSubsystems(subsystems);
            return subsystems.Count > 0 ? subsystems[0] : null;
        }
    }
}
