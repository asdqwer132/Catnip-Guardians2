using UnityEngine;

public class DecimalPlacesAttribute : PropertyAttribute
{
    public int decimalPlaces;

    public DecimalPlacesAttribute(int decimalPlaces = 2)
    {
        this.decimalPlaces = Mathf.Max(0, decimalPlaces);
    }
}