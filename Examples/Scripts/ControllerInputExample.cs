using Liminal.SDK.VR;
using Liminal.SDK.VR.Input;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class ControllerInputExample : MonoBehaviour
{
    public Text InputText;

    private void Update()
    {
        var device = VRDevice.Device;
        if (device != null)
        {
            StringBuilder inputStringBuilder = new StringBuilder("");

            AppendDeviceInput(inputStringBuilder, device.PrimaryInputDevice, "Primary");
            inputStringBuilder.AppendLine();
            AppendDeviceInput(inputStringBuilder, device.SecondaryInputDevice, "Secondary");

            InputText.text = inputStringBuilder.ToString();

        }
    }

    public void AppendDeviceInput(StringBuilder builder, IVRInputDevice inputDevice, string deviceName)
    {
        if (inputDevice == null)
            return;

        builder.AppendLine($"<b>{deviceName} Back:</b> {inputDevice.GetButton(VRButton.Back)}");
        builder.AppendLine($"<b>{deviceName} Button One:</b> {inputDevice.GetButton(VRButton.Primary)}");
        builder.AppendLine($"<b>{deviceName} Trigger:</b> {inputDevice.GetButton(VRButton.Trigger)}");
        builder.AppendLine($"<b>{deviceName} Touch Pad Touching:</b> {inputDevice.IsTouching}");

        builder.AppendLine($"<b>{deviceName} Axis One:</b> {inputDevice.GetAxis2D(VRAxis.One)}");
        builder.AppendLine($"<b>{deviceName} Axis One Raw:</b> {inputDevice.GetAxis2D(VRAxis.OneRaw)}");

        builder.AppendLine($"<b>{deviceName} Axis Two:</b> {inputDevice.GetAxis1D(VRAxis.Two)}");
        builder.AppendLine($"<b>{deviceName} Axis Two Raw:</b> {inputDevice.GetAxis1D(VRAxis.TwoRaw)}");

        builder.AppendLine($"<b>{deviceName} Axis Three:</b> {inputDevice.GetAxis1D(VRAxis.Three)}");
        builder.AppendLine($"<b>{deviceName} Axis Three Raw:</b> {inputDevice.GetAxis1D(VRAxis.ThreeRaw)}");


        if (inputDevice.GetButtonUp(VRButton.Trigger))
        {
            Debug.Log("Button up");
        }

        //builder.AppendLine($"{deviceName} Axis2D-One: {inputDevice.GetAxis2D(VRAxis.One)}");
        //builder.AppendLine($"{deviceName} Axis2D-OneRaw: {inputDevice.GetAxis2D(VRAxis.OneRaw)}");
    }
}
