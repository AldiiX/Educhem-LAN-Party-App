const paymentDeadlineFormatter = new Intl.DateTimeFormat('en-GB', {
    timeZone: 'Europe/Prague',
    day: 'numeric',
    month: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    hourCycle: 'h23',
})

export function arePaymentsAllowed(paymentDeadline: string, now = new Date()) {
    const deadlineTimestamp = new Date(paymentDeadline).getTime()

    return Number.isFinite(deadlineTimestamp) && now.getTime() <= deadlineTimestamp
}

export function formatPaymentDeadline(paymentDeadline: string) {
    const deadline = new Date(paymentDeadline)
    if (!Number.isFinite(deadline.getTime())) return paymentDeadline

    const parts = paymentDeadlineFormatter.formatToParts(deadline)
    const getPart = (type: Intl.DateTimeFormatPartTypes) => parts.find((part) => part.type === type)?.value ?? ''
    const day = Number(getPart('day'))
    const month = Number(getPart('month'))

    return `${day}.${month}. ${getPart('hour')}:${getPart('minute')}`
}
