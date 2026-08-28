import type {Metadata} from 'next'

export const metadata: Metadata = {
    title: 'Info',
    description: 'Důležité informace o platbě, rezervaci a pokynech pro účastníky EDUCHEM LAN Party.',
}

export default function({children}: { children: React.ReactNode }) {
    return children
}
