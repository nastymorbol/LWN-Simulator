package util

const (
	ConnectedPowerSource = 0x32

	MAXFCNTGAP = uint32(16384)

	PrintBoth = iota
	PrintOnlySocket
	PrintOnlyConsole

	//stati
	Stopped = iota
	Running

	Normal = iota
	Retransmission
	FPending
	Activation
)
