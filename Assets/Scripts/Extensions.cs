using Checkers;
using System.ComponentModel;
using UnityEngine;

public static class Extensions
{
   public static string ToCoordiante(this Transform transform)
    {
        return $"{transform.position.x},{transform.position.z}";
    }

    public static string ToSerializable(this string value, ColorType side, RecordType recordType,string destination="")
    {
        var playerSide = side == ColorType.Black ? "1" : "2";

        switch (recordType)
        {
            case RecordType.Click:
                return $"Player {playerSide} {recordType} to {value}";

            case RecordType.Move:
                return $"Player {playerSide} {recordType} from {value} to {destination}";

            case RecordType.Remove:
                return $"Player {playerSide} {recordType} chip at {value}";

            default:
                throw new InvalidEnumArgumentException($"Action {recordType} is not supported");
        }
    }
}
