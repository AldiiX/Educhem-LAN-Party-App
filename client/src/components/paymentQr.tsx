import type {PaymentStatus} from '@/lib/payments'

interface PaymentQrProps {
    enabled?: boolean
    status?: PaymentStatus
    startDateFormatted?: string
    message?: string
    imageClassName: string
    placeholderClassName: string
}

export function PaymentQr({
    enabled,
    status,
    startDateFormatted,
    message,
    imageClassName,
    placeholderClassName,
}: PaymentQrProps) {
    const resolvedStatus: PaymentStatus =
        status ?? (enabled === false ? 'closed' : 'active')

    if (resolvedStatus === 'upcoming') {
        return (
            <div className={placeholderClassName} role="status">
                {message ?? (startDateFormatted ? `Platby budou spuštěny ${startDateFormatted}.` : 'Platby ještě nebyly spuštěny.')}
            </div>
        )
    }

    if (resolvedStatus === 'closed') {
        return (
            <div className={placeholderClassName} role="status">
                {message ?? 'Platby již nejsou povoleny.'}
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

