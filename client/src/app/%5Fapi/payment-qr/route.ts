import 'server-only'

import QRCode from 'qrcode'
import {siteConfig} from '@/data/site'
import {arePaymentsAllowed} from '@/lib/payments'

export const runtime = 'nodejs'
export const dynamic = 'force-dynamic'

const paymentIban = 'CZ7220100000002603033660'

export async function GET() {
    const event = siteConfig.currentEvent

    if (!arePaymentsAllowed(event.paymentDeadline)) {
        return new Response(null, {
            status: 410,
            headers: {
                'Cache-Control': 'no-store',
            },
        })
    }

    const amount = event.feeDecimal.replace(',', '.').replace(/\s+CZK$/, '')
    const paymentData = [
        'SPD*1.0',
        `ACC:${paymentIban}`,
        `AM:${amount}`,
        'CC:CZK',
        `MSG:${event.paymentMessage}`,
    ].join('*')
    const svg = await QRCode.toString(paymentData, {
        type: 'svg',
        errorCorrectionLevel: 'M',
        margin: 2,
        width: 512,
        color: {
            dark: '#000000',
            light: '#ffffff',
        },
    })

    return new Response(svg, {
        headers: {
            'Cache-Control': 'no-store',
            'Content-Type': 'image/svg+xml; charset=utf-8',
            'X-Content-Type-Options': 'nosniff',
        },
    })
}
