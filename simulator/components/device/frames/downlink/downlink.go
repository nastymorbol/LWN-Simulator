package downlink

import (
	"errors"
	"fmt"

	"github.com/brocaar/lorawan"
)

// Downlink set with info of resp
type InformationDownlink struct {
	MType         lorawan.MType     `json:"-"` //per FPending
	FOptsReceived []lorawan.Payload `json:"-"`
	ACK           bool              `json:"-"`
	FPort         uint8             `json:"-"`
	DataPayload   []byte            `json:"-"`
	FPending      bool              `json:"-"`
	DwellTime     lorawan.DwellTime `json:"-"`
}

func GetDownlink(phy lorawan.PHYPayload, disableCounter bool, counter uint32, NwkSKey [16]byte, AppSKey [16]byte) (*InformationDownlink, uint32, error) {

	var downlink InformationDownlink
	var frameCounterError error

	//validate mic
	ok, err := phy.ValidateDownlinkDataMIC(lorawan.LoRaWAN1_0, 0, NwkSKey)
	if err != nil {
		return nil, 0, err
	}
	if !ok {
		return nil, 0, errors.New("Invalid MIC")
	}

	macPL, ok := phy.MACPayload.(*lorawan.MACPayload)
	if !ok {
		return nil, 0, errors.New("*MACPayload expected")
	}

	//validate counter
	if !disableCounter {

		if macPL.FHDR.FCnt != counter {
			// return nil, errors.New("Invalid downlink counter")
			frameCounterError = errors.New(fmt.Sprintf("Invalid downlink Counter: %d FHDR: %d", counter, macPL.FHDR.FCnt))
		}

	}

	if err := phy.DecodeFOptsToMACCommands(); err != nil {
		return nil, 0, err
	}

	downlink.MType = phy.MHDR.MType
	downlink.FPending = macPL.FHDR.FCtrl.FPending

	downlink.ACK = macPL.FHDR.FCtrl.ACK
	if macPL.FPort == nil {
		downlink.FPort = 0
	} else {
		downlink.FPort = *macPL.FPort
	}

	//MACCommand
	if len(macPL.FHDR.FOpts) != 0 {

		if macPL.FPort == nil || *macPL.FPort != uint8(0) { // MACCommand in Fopts
			if downlink.FOptsReceived != nil {
				downlink.FOptsReceived = append(downlink.FOptsReceived, macPL.FHDR.FOpts...)
			} else {
				downlink.FOptsReceived = macPL.FHDR.FOpts
			}
		}

	}

	if macPL.FPort != nil {

		switch *macPL.FPort {

		case uint8(0):
			//decrypt frame payload
			if err := phy.DecryptFRMPayload(NwkSKey); err != nil {
				return nil, 0, err
			}

			downlink.FOptsReceived = append(downlink.FOptsReceived, macPL.FRMPayload...)

		default:
			//Datapayload
			if err := phy.DecryptFRMPayload(AppSKey); err != nil {
				return nil, 0, err
			}

			pl, ok := macPL.FRMPayload[0].(*lorawan.DataPayload)
			if !ok {
				return nil, 0, errors.New("*DataPayload expected")
			}

			downlink.DataPayload = pl.Bytes

		}
	}

	return &downlink, macPL.FHDR.FCnt, frameCounterError
}
