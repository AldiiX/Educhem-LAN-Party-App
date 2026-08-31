import type { Metadata } from 'next';
export const metadata: Metadata = {
    title: 'Pokyny pro účastníky',
    description: 'Pokyny platné pro všechny účastníky akce. Přečtěte si je prosím pozorně.',
}

export default function({ children }: { children: React.ReactNode}) {
    return children;
}
