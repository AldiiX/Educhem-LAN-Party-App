interface PaymentQrProps {
    enabled: boolean
    imageClassName: string
    placeholderClassName: string
}

export function PaymentQr({enabled, imageClassName, placeholderClassName}: PaymentQrProps) {
    if (!enabled) {
        return (
            <div className={placeholderClassName} role="status">
                Platby již nejsou povoleny.
            </div>
        )
    }

    return (
        <img
            className={imageClassName}
            src="/_api/payment-qr"
            alt="QR kód pro platbu vstupného"
        />
    )
}
