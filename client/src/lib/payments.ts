const paymentDateFormatter = new Intl.DateTimeFormat('en-GB', {
    timeZone: 'Europe/Prague',
    day: 'numeric',
    month: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    hourCycle: 'h23',
})

export type PaymentStatus = 'upcoming' | 'active' | 'closed'

export function getPaymentStatus(
    paymentDeadline: string,
    paymentStartDate?: string,
    now = new Date()
): PaymentStatus {
    const nowTimestamp = now.getTime()
    const startTimestamp = paymentStartDate ? new Date(paymentStartDate).getTime() : NaN
    const deadlineTimestamp = new Date(paymentDeadline).getTime()

    if (Number.isFinite(startTimestamp) && nowTimestamp < startTimestamp) {
        return 'upcoming'
    }

    if (Number.isFinite(deadlineTimestamp) && nowTimestamp > deadlineTimestamp) {
        return 'closed'
    }

    return 'active'
}

export function arePaymentsAllowed(
    paymentDeadline: string,
    paymentStartDate?: string,
    now = new Date()
): boolean {
    return getPaymentStatus(paymentDeadline, paymentStartDate, now) === 'active'
}

export function formatPaymentDate(dateString: string): string {
    const date = new Date(dateString)
    if (!Number.isFinite(date.getTime())) return dateString

    const parts = paymentDateFormatter.formatToParts(date)
    const getPart = (type: Intl.DateTimeFormatPartTypes) => parts.find((part) => part.type === type)?.value ?? ''
    const day = Number(getPart('day'))
    const month = Number(getPart('month'))

    return `${day}.${month}. ${getPart('hour')}:${getPart('minute')}`
}

export const formatPaymentDeadline = formatPaymentDate
