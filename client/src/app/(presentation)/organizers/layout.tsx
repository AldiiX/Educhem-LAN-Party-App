import type { Metadata } from 'next';
export const metadata: Metadata = {
    title: 'Organizátoři',
    description: 'Kontakty na učitele, správce systému a další organizátory EDUCHEM LAN Party.',
}

export default function({ children }: { children: React.ReactNode}) {
    return children;
}
