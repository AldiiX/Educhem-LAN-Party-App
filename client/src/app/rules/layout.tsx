import type { Metadata } from 'next';
export const metadata: Metadata = {
    title: 'Pravidla',
    description: 'Pravidla platná pro všechny účastníky akce. Přečtěte si je prosím pozorně.',
}

export default function({ children }: { children: React.ReactNode}) {
    return children;
}