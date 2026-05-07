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
        paymentDeadline: '3.6. 20:00',
        venueShort: 'SŠ EDUCHEM',
        venueFull: 'SŠ EDUCHEM, Eduarda Basse 1142, 434 01 Most',
        fee: '100 Kč',
        feeDecimal: '100,00 CZK',
        bankAccount: '2603033660/2010',
        paymentMessage: 'JMÉNO PŘÍJMENÍ, TŘÍDA, EMAIL',
    },
    author: {
        name: 'Stanislav Škudrna',
        href: '/info#spravci',
    },
    repository: {
        label: 'GitHub',
        href: 'https://github.com/educhem/EDUCHEM-LAN-Party-Home',
    },
    navLinks: [
        {href: '/', label: 'Home'},
        {href: '/info', label: 'Info'},
        {href: '/history', label: 'Historie'},
        {href: '/reservation', label: 'Rezervace'},
        {href: '/rules', label: 'Pravidla'},
        {href: '/schedule', label: 'Harmonogram'},
        {href: '/faq', label: 'FAQ'},
    ],
}

export const historyEvents: HistoryEvent[] = [
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
            {src: '/images/history/xmas2025/2025.png', alt: '', tall: true},
            {src: '/images/history/xmas2025/1.png', alt: '', tall: true},
            {src: '/images/history/xmas2025/2.png', alt: '', tall: true},
            {src: '/images/history/xmas2025/3.png', alt: '', tall: true},
            {src: '/images/history/xmas2025/4.png', alt: '', tall: true},
            {src: '/images/history/xmas2025/IMG_1979.JPEG', alt: '', tall: true},
            {src: '/images/history/xmas2025/IMG_1989.JPEG', alt: '', tall: true},
            {src: '/images/history/xmas2025/IMG_2002.JPEG', alt: '', tall: true},
            {src: '/images/history/xmas2025/IMG_20251205_143544.jpg', alt: '', tall: true},
            {src: '/images/history/xmas2025/IMG_20251206_074657_781.jpg', alt: '', tall: true},
            {src: '/images/banner1.jpg', alt: '', wide: true},
            {src: '/images/img1.jpg', alt: ''},
            {src: '/images/img2.jpg', alt: ''},
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
            {src: '/images/history/xmas2024/1.jpg', alt: '', tall: true},
            {src: '/images/history/xmas2024/IMG_3442.jpeg', alt: '', tall: true},
            {src: '/images/history/xmas2024/IMG_3461.jpeg', alt: '', tall: true},
            {src: '/images/history/xmas2024/IMG_20241206_171706.jpg', alt: '', tall: true},
            {src: '/images/history/xmas2024/IMG_20241206_163848.jpg', alt: '', wide: true},
            {src: '/images/history/xmas2024/IMG_3448.jpeg', alt: '', wide: true},
            {src: '/images/history/xmas2024/IMG_20241206_183229.jpg', alt: '', tall: true},
            {src: '/images/history/xmas2024/IMG_20241206_210340.jpg', alt: '', tall: true},
            {src: '/images/history/xmas2024/IMG_20241206_210855.jpg', alt: '', tall: true},
            {src: '/images/history/xmas2024/IMG_20241206_205519.jpg', alt: '', wide: true},
            {src: '/images/banner2.jpeg', alt: '', wide: true},
            // {src: '/images/banner3.jpeg', alt: '', wide: true},
        ],
    },
    {
        title: 'Summer LAN',
        year: '2024',
        date: 'červen 2024',
        description: 'Letní akce, ze které se postupně skládá dnešní LAN Party systém a organizace.',
        links: [
            {label: 'Záznam Michalovo streamu', href: 'https://www.twitch.tv/videos/2167913910?filter=all&sort=time'},
        ],
        photos: [
            {src: '/images/history/summer2024/IMG_20240607_163202.jpg', alt: '', wide: true},
            {src: '/images/history/summer2024/IMG_20240607_170826.jpg', alt: '', tall: true},
            {src: '/images/history/summer2024/IMG_20240607_195534.jpg', alt: '', wide: true},
            {src: '/images/history/summer2024/IMG_20240607_205022.jpg', alt: '', tall: true},
            {src: '/images/history/summer2024/IMG_20240608_173830_903.jpg', alt: '', wide: true},
            {src: '/images/history/summer2024/IMG_20240607_225459.jpg', alt: '', tall: true},
            {src: '/images/history/summer2024/IMG_20240607_174530.jpg', alt: '', wide: true},
            {src: '/images/history/summer2024/IMG_20240608_001208.jpg', alt: '', tall: true},
        ],
    },
    {
        title: 'Christmas LAN',
        year: '2023',
        date: 'prosinec 2023',
        description: 'Vánoční edice školní LAN party s turnaji, volným hraním a večerním programem.',
        links: [],
        photos: [
            {src: '/images/history/xmas2023/Snapchat-1719190358.jpg', alt: '', tall: true},
            {src: '/images/history/xmas2023/IMG_20231215_225340.jpg', alt: '', tall: true},
            {src: '/images/history/xmas2023/Snapchat-799678916.jpg', alt: '', tall: true},
            {src: '/images/history/xmas2023/IMG_20231215_173415.jpg', alt: '', tall: true},
            {src: '/images/history/xmas2023/IMG_20231215_124206.jpg', alt: '', tall: true},
            {src: '/images/history/xmas2023/IMG_20231215_121550.jpg', alt: '', tall: true},
            {src: '/images/history/xmas2023/IMG_20231215_092945.jpg', alt: '', tall: true},

        ],
    },
    {
        title: 'LAN Party Strupčice',
        year: '2019',
        date: 'červen 2019',
        description: 'Třídenní LAN party v kulturním domě ve Strupčicích, která položila základy pro pozdější školní akce.',
        links: [],
        photos: [],
    },
]
