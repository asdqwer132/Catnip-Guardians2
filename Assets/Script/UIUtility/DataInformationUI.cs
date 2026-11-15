using UnityEngine;

public sealed class DataInformationUI : DataViewUI
{
    public BasicInformationModule basicInformation = new BasicInformationModule();
    protected override BasicInformationModule BasicInformation => basicInformation;
}
