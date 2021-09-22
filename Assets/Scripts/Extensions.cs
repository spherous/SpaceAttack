using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Extensions{
    public static class Extensions 
    {
        public static Vector3 GetScreenWrapPosition(this Vector3 screenPos, ScreenWrapIndicator indicators)
        {
            // Check horizontal wrapping
            if(screenPos.x < 0 && !indicators.leftLocked)
                screenPos = new Vector3(Screen.width + screenPos.x, screenPos.y, screenPos.z);
            else if(screenPos.x > Screen.width && !indicators.rightLocked)
                screenPos = new Vector3(screenPos.x - Screen.width, screenPos.y, screenPos.z);

            // Check vertical wrapping
            if(screenPos.y < 0 && !indicators.topLocked)
                screenPos = new Vector3(screenPos.x, Screen.height + screenPos.y, screenPos.z);
            else if(screenPos.y > Screen.height && !indicators.bottomLocked)
                screenPos = new Vector3(screenPos.x, screenPos.y - Screen.height, screenPos.z);
            
            return screenPos;
        }

        public static Vector2 GetRandomOffScreenLocationOnSide(int sideID)
        {
            return sideID switch{
                0 => new Vector3(UnityEngine.Random.Range(0, Screen.width), Screen.height - 50),// top
                1 => new Vector3(Screen.width + 50, UnityEngine.Random.Range(0, Screen.height)),// right
                2 => new Vector3(UnityEngine.Random.Range(0, Screen.width), Screen.height + 50),// bottom
                3 => new Vector3(Screen.width - 50, UnityEngine.Random.Range(0, Screen.height)),// left
                _ => Vector2.zero
            };
        }

        public static (float x, float y) GetRandomOffScreenLocation()
        {
            //  Choose a random position off the edge of the screen
            int offEdge = UnityEngine.Random.Range(0, 2);
            int posOrNeg = UnityEngine.Random.Range(0, 2);
            float offset = UnityEngine.Random.Range(30, 40);

            return offEdge switch{
                // off height
                0 => (UnityEngine.Random.Range(0f, Screen.width), posOrNeg == 0 ? Screen.height + offset : 0 - offset),
                // off width
                1 => (posOrNeg == 0 ? Screen.width + offset : 0 - offset, UnityEngine.Random.Range(0f, Screen.height)),
                _ => (0,0)
            };
        }

        public static bool IsOffScreen(this Vector3 screenPos) => 
            screenPos.x < 0 || screenPos.y < 0 || screenPos.x > Screen.width || screenPos.y > Screen.height;

        public static float PercentInRange(this float from, float toLimit1, float toLimit2) => toLimit1 + ((toLimit2 - toLimit1) * Mathf.Clamp(from, 0, 1));
        public static float Remap(this float from, float fromLimit1, float fromLimit2, float toLimit1, float toLimit2) =>
            PercentInRange(
                (Mathf.Clamp(from, fromLimit1, fromLimit2) - fromLimit1)/(fromLimit2 - fromLimit1), 
                toLimit1, toLimit2
            );

        public static T ChooseRandom<T>(this List<T> set) => set[UnityEngine.Random.Range(0, set.Count)];
        public static T ChooseRandom<T>(this T[] set) => set[UnityEngine.Random.Range(0, set.Length)];

        public static bool RandomBool() => UnityEngine.Random.Range(0, 2) == 0;

        public static float Dot(this Vector3 v1, Vector3 v2) => Vector3.Dot(v1, v2);

    }
    // 1100, 0000, 0100
    enum TriSign : sbyte {Negative = -1, Zero = 0, Positive = 1}
}