namespace Prospect.Unreal.Core.Math;

public class FVector
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    
    public FVector()
    {
        X = 0;
        Y = 0;
        Z = 0;
    }
    
    public FVector(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }
    
    public static FVector Zero => new FVector(0, 0, 0);
    public static FVector One => new FVector(1, 1, 1);
}