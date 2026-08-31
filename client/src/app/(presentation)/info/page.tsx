'use client'

import {useState} from 'react'
import {Accordion, AccordionItem} from '@/components/accordion'
import {siteConfig} from '@/data/site'
import shell from '../page-shell.module.scss'
import styles from './info.module.scss'
import {PaymentQr} from '@/components/paymentQr'
import {arePaymentsAllowed, formatPaymentDeadline} from '@/lib/payments'

interface RuleCategory {
    id: string
    title: string
    rules: { title: string; content: string }[]
    image?: { src: string; alt: string; caption: string }
}

const event = siteConfig.currentEvent

const getReservationSteps = (paymentsAllowed: boolean, paymentDeadline: string) => [
    {
        title: `Zaplaťte vstupné ${event.fee}`,
        details: paymentsAllowed
            ? [
                `Číslo účtu: ${event.bankAccount}`,
                `Částka: ${event.feeDecimal}`,
                `Zpráva pro příjemce: ${event.paymentMessage}`,
                'Dodržujte prosím tento formát.',
                `Termín: do ${paymentDeadline}`,
                'Můžete zaplatit ručním zadáním, nebo pomocí QR kódu.',
                'Platby k účastníkům přiřazujeme ručně, proto se přístup může objevit až do 2 pracovních dnů.',
            ]
            : [
                `Číslo účtu: [NEDOSTUPNÉ, PLATBY UKONČENY ${paymentDeadline}]`,
                `Částka: ${event.feeDecimal}`,
                `Zpráva pro příjemce: ${event.paymentMessage}`,
                'Dodržujte prosím tento formát.',
                `Termín: do ${paymentDeadline}`,
                'Můžete zaplatit ručním zadáním, nebo pomocí QR kódu.',
                'Platby k účastníkům přiřazujeme ručně, proto se přístup může objevit až do 2 pracovních dnů.',
            ],
    },
    {
        title: 'Obdržíte přístupové údaje',
        details: [
            'Jakmile zaplatíte, budete mít možnost rezervovat své místo v LAN Party systému.',
            'Přístupové údaje vám přijdou do emailu, který jste uvedli ve zprávě platby.',
            'Přiřazování plateb probíhá manuálně a může trvat až 2 pracovní dny.',
        ],
    },
    {
        title: 'Rezervujte si místo v systému',
        details: [
            'V systému na stránce /app/reservations si můžete rezervovat místo nebo počítač.',
            'Pokud si neplánujete brát s sebou PC ani být na školním PC, nemusíte si místo rezervovat.',
        ],
    },
]

const getReservationFaq = (paymentsAllowed: boolean, paymentDeadline: string) => [
    {
        question: 'Musím si rezervovat místo?',
        answer: 'Kvůli velkému počtu účastníků je ideální rezervovat si počítač nebo místo pro vlastní setup. Pokud si neplánujete brát s sebou PC ani být na školním PC, nemusíte si místo rezervovat.',
    },
    {
        question: 'Může se moje místo změnit?',
        answer: 'Ano, může se stát, že vaše místo bude změněno, protože ještě mohou proběhnout úpravy. Často dáváme spolužáky vedle sebe nebo do stejných tříd.',
    },
    {
        question: 'Co když budu mít problém se systémem?',
        answer: 'V případě jakéhokoli problému se systémem kontaktujte správce: Stanislav Škudrna (@aldiix) nebo Serhii Yavorskyi (@_.yavorskiy.s._).',
    },
    {
        question: 'Mohu přijít a odejít kdykoliv?',
        answer: 'Ano, můžete přijít/odejít kdykoliv během akce. Odchod ale musíte dát vědět někomu z učitelů, ideálně napsat na školní Discord.',
    },
    {
        question: 'Do kdy musím zaplatit?',
        answer: paymentsAllowed
            ? `Vstupné ${event.fee} je nutné zaplatit do ${paymentDeadline}.`
            : `Termín pro platbu skončil ${paymentDeadline}.`,
    },
]

const categories: RuleCategory[] = [
    {
        id: 'bezpecnost',
        title: 'Bezpečnost a technika',
        rules: [
            {
                title: 'Bezpečnostní opatření',
                content: 'Po celou dobu konání akce dodržujte bezpečnostní pokyny. Nepoužívejte elektroniku nebo jiná zařízení tak, aby to ohrozilo vás nebo ostatní.'
            },
            {
                title: 'Odcházení z budovy',
                content: 'Odcházení během akce z budovy školy je možné, ale učitel musí být informován. Účastník je povinen zapsat svůj odchod i následný příchod v aplikaci v sekci Docházka.'
            },
            {
                title: 'Evidence příchodů a odchodů',
                content: 'Každý účastník je povinen v aplikaci zapisovat příchody a odchody z akce včetně důvodu odchodu, aby organizátoři měli aktuální přehled o tom, kdo se nachází na akci.'
            },
            {
                title: 'Technická zařízení',
                content: 'Není dovoleno měnit zapojení školních PC (odpojovat monitory) či jiné periferie včetně myší a klávesnice. Můžete si ale připojit vlastní myš/sluchátka/klávesnice do VOLNÝCH portů.'
            },
            {title: 'Cizí vybavení', content: 'Prosíme, nezasahujte do cizího vybavení bez svolení majitele.'},
            {
                title: 'Vlastní setup',
                content: 'Účastníci si mohou vzít vlastní setup. Jsou povinni vzít si vlastní monitor, prodlužovák a veškeré věci potřebné pro chod počítače.'
            },
            {
                title: 'Aktualizovaný software',
                content: 'Veškerý software nainstalovaný na vašem setupu musí být aktualizovaný, včetně samotného operačního systému.'
            },
        ],
    },
    {
        id: 'majetek',
        title: 'Ochrana majetku a prostředí',
        rules: [
            {
                title: 'Respekt k majetku',
                content: 'Nepoužívejte věci ostatních účastníků bez jejich souhlasu. Každý účastník nese odpovědnost za své osobní věci.'
            },
            {
                title: 'Čistota a pořádek',
                content: 'Udržujte prostor, kde se akce koná, v čistotě. Po sobě uklízejte a odstraňujte nepořádek. Předtím, než budete z akce odcházet, si po sobě ukliďte.'
            },
        ],
    },
    {
        id: 'chovani',
        title: 'Komunikace a chování',
        rules: [
            {
                title: 'Respektujte ostatní účastníky',
                content: 'Buďte ohleduplní a respektujte hranice a pohodlí ostatních. Neprovádějte žádné nevhodné nebo rušivé chování.'
            },
            {
                title: 'Hlučnost a klidová doba',
                content: 'V noci snižte hlasitost, abyste minimalizovali rušení okolního prostředí během nočního klidu.'
            },
        ],
    },
    {
        id: 'jidlo',
        title: 'Jídlo a nápoje',
        rules: [
            {
                title: 'Pokyny ke stravování',
                content: 'Dodržujte pokyny ohledně jídla a pití stanovené školou/pořadatelem. Jezte a pijte tak, abyste neohrozili majetek účastníků a školy.'
            },
            {
                title: 'Čas jídla',
                content: 'Na jídlo není stanoven přesný čas, jíst se bude v daný čas, kdy to vyjde. Jídlo na grilování a pití je v ceně.'
            },
            {title: 'Výdej jídla', content: 'Jídlo vám vždy vydá grillmaster.'},
            {
                title: 'Kontrola masa',
                content: 'Zkontrolujte si, především ve večerních hodinách, že maso není syrové. Pokud bude syrové, vraťte ho grillmasterovi na dodělání.'
            },
            {title: 'Chování u grilu', content: 'Dodržujte zásady slušného chování u grilu.'},
        ],
    },
    {
        id: 'hry',
        title: 'Jakým způsobem stahovat hry',
        image: {
            src: '/images/guides/steam-download-settings.webp',
            alt: 'Nastavení Steamu pro povolení přenosu her po místní síti pro kohokoli',
            caption: 'Ve Steamu otevřete Nastavení, Stahování a povolte přenos souborů her po místní síti pro kohokoli.',
        },
        rules: [
            {
                title: 'Opatření pro stahování',
                content: 'Kvůli přetížení sítě jsme museli udělat opatření pro stahování her. Pro snížení přetížení sítě si zkontrolujte a případně zapněte příslušné nastavení na Steamu na školním počítači.'
            },
            {
                title: 'Doporučení - vlastní disk',
                content: 'Doporučujeme mít vlastní externí HDD/SSD, na kterém máte nainstalované hry. Disk si můžete přinést a hry spustit přímo z něj.'
            },
        ],
    },
    {
        id: 'zaverecne',
        title: 'Závěrečné pokyny',
        rules: [
            {
                title: 'Pravomoc organizátorů',
                content: 'Organizátoři mají právo řešit jakékoliv problémy nebo nesrovnalosti, aby zajistili plynulý průběh akce a pohodu všech účastníků.'
            },
            {title: 'Poděkování', content: 'Děkujeme vám za vaši účast!'},
        ],
    },
]

const tocItems = [
    {id: 'reservation', label: 'Rezervace'},
    {id: 'instructions', label: 'Pokyny pro účastníky'},
    ...categories.map((cat) => ({id: cat.id, label: cat.title})),
]

export default function() {
    const paymentsAllowed = arePaymentsAllowed(event.paymentDeadline)
    const paymentDeadline = formatPaymentDeadline(event.paymentDeadline)
    const reservationSteps = getReservationSteps(paymentsAllowed, paymentDeadline)
    const reservationFaq = getReservationFaq(paymentsAllowed, paymentDeadline)
    const [search, setSearch] = useState('')
    const query = search.toLowerCase()
    const filteredCategories = categories
        .map((cat) => ({
            ...cat,
            rules: cat.rules.filter(
                (r) =>
                    r.title.toLowerCase().includes(query) ||
                    r.content.toLowerCase().includes(query) ||
                    cat.title.toLowerCase().includes(query)
            ),
        }))
        .filter((cat) => cat.rules.length > 0)

    return (
        <>
            <div className={shell.page}>
                <div className={shell.pageHeader}>
                    <span className={shell.eyebrow}>Info</span>
                    <h1 className={shell.title}>Důležité informace</h1>
                    <p className={shell.description}>
                        {event.venueFull}. Akce probíhá od {event.startDate} {event.startTime} do {event.endDate}{' '}
                        {event.endTime} a časy jsou orientační.
                    </p>
                </div>

                <div className={shell.twoCol}>
                    <aside className={shell.stickySidebar}>
                        <nav className={shell.toc}>
                            <h2 className={shell.tocTitle}>Obsah</h2>
                            <ul className={shell.tocList}>
                                {tocItems.map((item) => (
                                    <li key={item.id}>
                                        <a href={`#${item.id}`} className={shell.tocLink}>{item.label}</a>
                                    </li>
                                ))}
                            </ul>
                        </nav>
                    </aside>

                    <div>
                        <section id="reservation" className={`${shell.section} ${styles.reservation}`}>
                            <h2 className={shell.sectionTitle}>
                                <span className={shell.sectionMark} aria-hidden="true"/>
                                Rezervace a platba
                            </h2>
                            <p className={styles.reservationIntro}>
                                Rezervace je určená pro účastníky, kteří chtějí mít jistotu školního PC nebo místa pro
                                vlastní setup.
                            </p>
                            <a href="/app/reservations" className={`${shell.button} ${shell.primaryButton}`}>
                                Vstup do rezervačního systému
                            </a>

                            <div className={styles.steps}>
                                {reservationSteps.map((step, idx) => (
                                    <div key={idx} className={`${shell.card} ${styles.step}`}>
                                        <div className={styles.number}>{idx + 1}</div>
                                        <div>
                                            <h3 className={styles.stepTitle}>{step.title}</h3>
                                            <ul className={styles.details}>
                                                {step.details.map((detail, detailIndex) => (
                                                    <li key={detailIndex} className={styles.detail}>
                                                        <span className={styles.bullet} aria-hidden="true"/>
                                                        {detail}
                                                    </li>
                                                ))}
                                            </ul>
                                            {idx === 0 && (
                                                <div className={styles.stepQr}>
                                                    <div>
                                                        <p className={styles.stepQrTitle}>Platba QR kódem</p>
                                                        <p className={styles.stepQrText}>
                                                            {paymentsAllowed ? (
                                                                <>
                                                                    Naskenujte QR kód a před odesláním zkontrolujte
                                                                    zprávu pro příjemce. Platba musí být odeslaná
                                                                    do {paymentDeadline}.
                                                                </>
                                                            ) : (
                                                                <>
                                                                    Naskenujte QR kód a před odesláním zkontrolujte
                                                                    zprávu pro příjemce. Termín pro platbu skončil {paymentDeadline}.
                                                                </>
                                                            )}
                                                        </p>
                                                    </div>
                                                    <PaymentQr
                                                        enabled={paymentsAllowed}
                                                        imageClassName={styles.qrCode}
                                                        placeholderClassName={styles.qrPlaceholder}
                                                    />
                                                </div>
                                            )}
                                        </div>
                                    </div>
                                ))}
                            </div>

                            <figure className={shell.contentMedia}>
                                <img
                                    src="/images/guides/reservation-system.webp"
                                    alt="Ukázka výběru počítače a tlačítka Rezervovat v LAN Party systému"
                                    className={shell.contentImage}
                                />
                                <figcaption className={shell.contentCaption}>
                                    Po výběru volného místa nebo počítače použijte tlačítko Rezervovat.
                                </figcaption>
                            </figure>

                            <div className={styles.alerts}>
                                {paymentsAllowed && (
                                    <div className={`${shell.alert} ${shell.alertError}`}>
                                        <div className={shell.alertIcon} aria-hidden="true">!</div>
                                        <div>
                                            <p className={shell.alertTitle}>Důležité upozornění</p>
                                            <p className={shell.alertDescription}>
                                                Ve zprávě pro příjemce dodržujte formát: {event.paymentMessage}. Bez
                                                správného formátu nemusí být platba přiřazena. Potvrzení může kvůli
                                                ručnímu přiřazování trvat až 2 pracovní dny.
                                            </p>
                                        </div>
                                    </div>
                                )}
                                <div className={shell.alert}>
                                    <div className={shell.alertIcon} aria-hidden="true">!</div>
                                    <div>
                                        <p className={shell.alertTitle}>Změna místa</p>
                                        <p className={shell.alertDescription}>
                                            Vaše místo může být změněno kvůli úpravám rozložení, například když dáváme
                                            spolužáky vedle sebe.
                                        </p>
                                    </div>
                                </div>
                            </div>

                            <section className={styles.faq}>
                                <h3 className={shell.sectionTitle}>
                                    <span className={shell.sectionMark} aria-hidden="true"/>
                                    Časté dotazy k rezervaci
                                </h3>
                                <Accordion>
                                    {reservationFaq.map((item, idx) => (
                                        <AccordionItem key={idx} title={item.question}>
                                            <p>{item.answer}</p>
                                        </AccordionItem>
                                    ))}
                                </Accordion>
                            </section>
                        </section>

                        <section id="instructions" className={shell.section}>
                            <h2 className={shell.sectionTitle}>
                                <span className={shell.sectionMark} aria-hidden="true"/>
                                Pokyny pro účastníky
                            </h2>
                            <div className={shell.search}>
                                <span className={shell.searchIcon} aria-hidden="true"/>
                                <input
                                    type="text"
                                    className={shell.input}
                                    placeholder="Hledat v pokynech..."
                                    value={search}
                                    onChange={(e) => setSearch(e.target.value)}
                                />
                            </div>
                            <div className={`${shell.alert} ${shell.leadAlert}`}>
                                <div className={shell.alertIcon} aria-hidden="true">!</div>
                                <div>
                                    <p className={shell.alertTitle}>Důležité</p>
                                    <p className={shell.alertDescription}>
                                        Dodržování pokynů je povinné pro všechny účastníky. Organizátoři mají právo
                                        řešit problémy a nesrovnalosti pro zajištění plynulého průběhu akce.
                                    </p>
                                </div>
                            </div>
                        </section>

                        {filteredCategories.length === 0 ? (
                            <p className={shell.empty}>Žádné výsledky pro zadaný hledaný výraz.</p>
                        ) : (
                            filteredCategories.map((cat) => (
                                <section key={cat.id} id={cat.id} className={shell.section}>
                                    <h2 className={shell.sectionTitle}>
                                        <span className={shell.sectionMark} aria-hidden="true"/>
                                        {cat.title}
                                    </h2>
                                    <Accordion>
                                        {cat.rules.map((rule, idx) => (
                                            <AccordionItem key={idx} title={rule.title} defaultOpen={idx === 0}>
                                                <p>{rule.content}</p>
                                            </AccordionItem>
                                        ))}
                                    </Accordion>
                                    {cat.image && (
                                        <figure className={shell.contentMedia}>
                                            <img src={cat.image.src} alt={cat.image.alt} className={shell.contentImage}/>
                                            <figcaption className={shell.contentCaption}>{cat.image.caption}</figcaption>
                                        </figure>
                                    )}
                                </section>
                            ))
                        )}
                    </div>
                </div>
            </div>
        </>
    )
}
