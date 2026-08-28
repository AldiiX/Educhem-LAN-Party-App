type HistoryPhoto = {
    src: string
    alt: string
    tall?: boolean
    wide?: boolean
}

type HistoryEvent = {
    title: string
    year: string
    date: string
    description: string
    links: {label: string; href: string}[]
    photos: HistoryPhoto[]
}

export const siteConfig = {
    brandName: 'EDUCHEM LAN Party',
    currentEvent: {
        name: 'Summer LAN Party',
        year: '2026',
        title: 'Summer LAN Party 2026',
        heroTitle: 'Summer',
        heroAccent: 'LAN Party',
        dateLong: '5. - 6. června 2026',
        dateShort: '5. - 6. 6. 2026',
        startDate: '5.6.',
        endDate: '6.6.',
        startTime: '12:00',
        endTime: '14:00',
        paymentDeadline: '2026-06-03T20:00:00+02:00',
        venueShort: 'SŠ EDUCHEM',
        venueFull: 'SŠ EDUCHEM, Eduarda Basse 1142, 434 01 Most',
        fee: '100 Kč',
        feeDecimal: '100,00 CZK',
        bankAccount: '2603033660/2010',
        paymentMessage: 'JMÉNO PŘÍJMENÍ, TŘÍDA, EMAIL',
    },
    author: {
        name: 'Stanislav Škudrna',
        href: '/organizers#spravci',
    },
    repository: {
        label: 'GitHub',
        href: 'https://github.com/AldiiX/Educhem-LAN-Party-App',
    },
    navLinks: [
        {href: '/', label: 'Home'},
        {href: '/info', label: 'Info'},
        {href: '/organizers', label: 'Organizátoři'},
        {href: '/history', label: 'Historie'},
        {href: '/schedule', label: 'Harmonogram'},
        {href: '/faq', label: 'FAQ'},
    ],
}

export const historyEvents: HistoryEvent[] = [
    {
        title: 'Summer LAN Party',
        year: '2026',
        date: '5. - 6. června 2026',
        description: 'Letní LAN party s volným hraním, volejbalem a večerním programem.',
        links: [
        ],
        photos: [
            {src: '/images/history/summer2026/1.webp', alt: '', wide: true },
            {src: '/images/history/summer2026/2.webp', alt: ''},
            {src: '/images/history/summer2026/3.webp', alt: ''},
            {src: '/images/history/summer2026/4.webp', alt: ''},
        ],
    },

    {
        title: 'Mikulášská LAN Party',
        year: '2025',
        date: '5. - 6. prosince 2025',
        description: 'Zimní LAN party s předvánoční atmosférou, společným hraním a streamem.',
        links: [
            {label: 'Michalův stream', href: 'https://www.youtube.com/playlist?list=PLUIcP-krTh9CaeqcdZ3supr8Cawg28Y46'},
            {label: 'Aftermovie', href: 'https://drive.google.com/file/d/1wPv4upVD-lB3YoJMlur7zaLkkliFXHqX/view?usp=sharing'},
            {label: 'Výsledky turnaje CS2', href: 'https://www.copafacil.com/-v9yer2'},
        ],
        photos: [
            {src: '/images/history/xmas2025/1.webp', alt: '', tall: true},
            {src: '/images/history/xmas2025/2.webp', alt: '', tall: true},
            {src: '/images/history/xmas2025/3.webp', alt: '', tall: true},
            {src: '/images/history/xmas2025/4.webp', alt: '', tall: true},
            {src: '/images/history/xmas2025/5.webp', alt: '', tall: true},
            {src: '/images/history/xmas2025/6.webp', alt: '', tall: true},
            {src: '/images/history/xmas2025/7.webp', alt: '', tall: true},
            {src: '/images/history/xmas2025/8.webp', alt: '', tall: true},
            {src: '/images/history/xmas2025/9.webp', alt: '', tall: true},
            {src: '/images/history/xmas2025/10.webp', alt: '', tall: true},
            {src: '/images/banner1.webp', alt: '', wide: true},
            {src: '/images/img1.webp', alt: ''},
            {src: '/images/img2.webp', alt: ''},
        ],
    },

    {
        title: 'Christmas LAN',
        year: '2024',
        date: 'prosinec 2024',
        description: 'Vánoční edice školní LAN party s turnaji, volným hraním a večerním programem.',
        links: [
        ],
        photos: [
            {src: '/images/history/xmas2024/1.webp', alt: '', tall: true},
            {src: '/images/history/xmas2024/2.webp', alt: '', tall: true},
            {src: '/images/history/xmas2024/3.webp', alt: '', tall: true},
            {src: '/images/history/xmas2024/4.webp', alt: '', tall: true},
            {src: '/images/history/xmas2024/5.webp', alt: '', wide: true},
            {src: '/images/history/xmas2024/6.webp', alt: '', wide: true},
            {src: '/images/history/xmas2024/7.webp', alt: '', tall: true},
            {src: '/images/history/xmas2024/8.webp', alt: '', tall: true},
            {src: '/images/history/xmas2024/9.webp', alt: '', tall: true},
            {src: '/images/history/xmas2024/10.webp', alt: '', wide: true},
            {src: '/images/banner2.webp', alt: '', wide: true},
            // {src: '/images/banner3.webp', alt: '', wide: true},
        ],
    },
    {
        title: 'Summer LAN',
        year: '2024',
        date: 'červen 2024',
        description: 'Letní akce s turnaji, grillem, posezením venku a večerním programem (hraní na kytaru, ...)',
        links: [
            {label: 'Záznam Michalovo streamu', href: 'https://www.twitch.tv/videos/2167913910?filter=all&sort=time'},
        ],
        photos: [
            {src: '/images/history/summer2024/1.webp', alt: '', wide: true},
            {src: '/images/history/summer2024/2.webp', alt: '', tall: true},
            {src: '/images/history/summer2024/3.webp', alt: '', wide: true},
            {src: '/images/history/summer2024/4.webp', alt: '', tall: true},
            {src: '/images/history/summer2024/5.webp', alt: '', wide: true},
            {src: '/images/history/summer2024/6.webp', alt: '', tall: true},
            {src: '/images/history/summer2024/7.webp', alt: '', wide: true},
            {src: '/images/history/summer2024/8.webp', alt: '', tall: true},
        ],
    },
    {
        title: 'Christmas LAN',
        year: '2023',
        date: 'prosinec 2023',
        description: 'Vánoční edice školní LAN party s turnaji, volným hraním a večerním programem.',
        links: [],
        photos: [
            {src: '/images/history/xmas2023/1.webp', alt: '', tall: true},
            {src: '/images/history/xmas2023/2.webp', alt: '', tall: true},
            {src: '/images/history/xmas2023/3.webp', alt: '', tall: true},
            {src: '/images/history/xmas2023/4.webp', alt: '', tall: true},
            {src: '/images/history/xmas2023/5.webp', alt: '', tall: true},
            {src: '/images/history/xmas2023/6.webp', alt: '', tall: true},
            {src: '/images/history/xmas2023/7.webp', alt: '', tall: true},

        ],
    },
    {
        title: 'LAN Party Strupčice',
        year: '2019',
        date: 'červen 2019',
        description: 'Třídenní LAN party v kulturním domě ve Strupčicích. Úplně první LAN Party pořádaná naší školou.',
        links: [],
        photos: [],
    },
]
