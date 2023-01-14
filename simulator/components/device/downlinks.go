package device

import (
	"github.com/arslab/lwnsimulator/simulator/util"

	act "github.com/arslab/lwnsimulator/simulator/components/device/activation"
	"github.com/arslab/lwnsimulator/simulator/components/device/classes"
	dl "github.com/arslab/lwnsimulator/simulator/components/device/frames/downlink"
	"github.com/brocaar/lorawan"
)

func (d *Device) ProcessDownlink(phy lorawan.PHYPayload) (*dl.InformationDownlink, error) {

	var payload *dl.InformationDownlink
	var err error
	var fcnt uint32

	mtype := phy.MHDR.MType
	err = nil

	switch mtype {

	case lorawan.JoinAccept:
		Ja, err := act.DecryptJoinAccept(phy, d.Info.DevNonce, d.Info.JoinEUI, d.Info.AppKey)
		if err != nil {
			return nil, err
		}
		d.Info.Status.FCntDown = 0
		return d.ProcessJoinAccept(Ja)

	case lorawan.UnconfirmedDataDown, lorawan.ConfirmedDataDown:

		payload, fcnt, err = dl.GetDownlink(phy, d.Info.Configuration.DisableFCntDown, d.Info.Status.FCntDown,
			d.Info.NwkSKey, d.Info.AppSKey)

		if err != nil {
			//d.Print("Downlink Error: ", err, util.PrintBoth)
			//return nil, err
			d.Info.Status.FCntDown = fcnt
		}

		d.Info.Status.FCntDown = (d.Info.Status.FCntDown + 1) % util.MAXFCNTGAP

		if mtype == lorawan.ConfirmedDataDown {
			d.SendAck()
		}

		if payload.DataPayload != nil {
			d.PrintDownlink(payload.FPort, payload.DataPayload)
		}
	}

	switch d.Class.GetClass() {

	case classes.ClassA:
		d.Info.Status.DataUplink.AckMacCommand.CleanFOptsRXParamSetupAns()
		d.Info.Status.DataUplink.AckMacCommand.CleanFOptsRXTimingSetupAns()
		break

	case classes.ClassC:
		d.Info.Status.InfoClassC.SetACK(false) //Reset

	}

	msg := d.Info.Status.DataUplink.ADR.Reset()
	if msg != "" {
		d.Print(msg, nil, util.PrintBoth)
	}

	d.Info.Status.DataUplink.AckMacCommand.CleanFOptsDLChannelAns()

	return payload, err
}
