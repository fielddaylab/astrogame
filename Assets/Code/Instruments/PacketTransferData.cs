using Astro;
using BeauUtil;
using FieldDay;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class PacketTransferDataUtility
{
    private static PacketTransferData m_WorkingPacketTransferData = new PacketTransferData();
    private static StringBuilder m_WorkingStringBuilder = new StringBuilder();

    public static void DispatchEvent(DataSlot dataSlot, StringHash32 eventId, string starId = "") {
        m_WorkingStringBuilder.Clear();
        // Assemble Analytics data
        if (starId != "") {
            m_WorkingPacketTransferData.StarId = starId;
        }
        m_WorkingPacketTransferData.ToolId = DataUtility.GetInstrumentTypeFromDataMask(dataSlot.Type);

        if (dataSlot.CurrentData.IsValid) {
            if (DataUtility.TryFormatForDefaultOutput(dataSlot.CurrentData, dataSlot.Displays[0].Formatting, m_WorkingStringBuilder)) {
                m_WorkingPacketTransferData.ValueStr = m_WorkingStringBuilder.ToString();
            }
        } else {
            string nullTxt = dataSlot.Displays[0].NullText;
            if (string.IsNullOrEmpty(nullTxt)) {
                nullTxt = DataUtility.EMPTY_OUTPUT;
            }
            m_WorkingStringBuilder.Append(nullTxt);
        }
        m_WorkingPacketTransferData.ValueStr = m_WorkingStringBuilder.ToString();

        AstroGame.Events.Dispatch(eventId, EvtArgs.Box(m_WorkingPacketTransferData));
    }

}
