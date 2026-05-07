import type { Metadata } from 'next';
export const metadata: Metadata = {
    title: 'Info',
    description: 'Všechny důležité informace o akci, včetně kontaktů, platebních údajů a dalších detailů.',
}

export default function({ children }: { children: React.ReactNode}) {
    return children;
}