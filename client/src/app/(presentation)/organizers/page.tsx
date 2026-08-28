'use client'

import {useState} from 'react'
import {siteConfig} from '@/data/site'
import shell from '../page-shell.module.scss'
import style from './organizers.module.scss'
import If from "@/components/util/If";
import isNullOrUndefined from "@/lib/isNullOrUndefined";

const tocItems = [
    {id: 'ucitele', label: 'Učitelé'},
    {id: 'spravci', label: 'Správci LAN Party systému'},
    {id: 'organizatoriturnaju', label: 'Organizátoři turnajů'},
    {id: 'grillmasteri', label: 'Grillmasteři'},
    {id: 'kontakt', label: 'Kontakt'},
]

interface Organizer {
    name: string;
    role: string;
    phone?: string;
    instagram?: string | null;
    discord?: string | null;
    avatarUrl?: string | null;
    category: 'teacher' | 'admin' | 'grillmaster' | "tournaments";
}

const organizers: Organizer[] = [ // TODO: tahat toto z db, ne takto hardcoded
    {
        name: 'Michaela Mudrochová',
        role: 'Učitelka',
        phone: '+420 777 131 303',
        discord: 'micha_cz',
        category: 'teacher',
        avatarUrl: "https://cloud02.emsio.cz/public/avatars/17111c13-da60-47c0-b436-64b2c39e584e.jpg",
    },
    {
        name: 'Michal Mudroch Bureš',
        role: 'Učitel',
        phone: '+420 777 116 567',
        discord: 'deathwalker_cz',
        category: 'teacher',
        avatarUrl: "https://cloud02.emsio.cz/public/avatars/874350a8-a348-4036-9af4-4e10b4780861.png",
    },
    {
        name: 'Sebastian Netolický',
        role: 'Učitel',
        discord: 'internal_server_error.',
        category: 'teacher',
        avatarUrl: "https://cloud02.emsio.cz/public/avatars/1682759192302.jpg",
    },
    {name: 'David Chlad', role: 'Učitel', discord: 'ampercz1', category: 'teacher', avatarUrl: "https://cloud02.emsio.cz/public/avatars/649d2825-ca12-4b48-8e00-391b42422897.png"},
    {name: 'Karel Honsig', role: 'Učitel', phone: '+420 724 478 552', discord: 'karelhonsig', category: 'teacher', avatarUrl: "https://cloud02.emsio.cz/public/avatars/f997e1dc-2467-45be-bc13-3d0c58a0c424.png",},
    {name: 'Stanislav Škudrna', role: 'Správce LAN Party systému', instagram: 'stanley.sku', discord: 'aldiix', category: 'admin', avatarUrl: "https://cloud02.emsio.cz/public/avatars/stanislavskudrna.png"},
    {name: 'Serhii Yavorskyi', role: 'Správce LAN Party systému', discord: '_.yavorskiy.s._', instagram: '_.yavorskiy.s._', category: 'admin', avatarUrl: "https://cloud02.emsio.cz/public/avatars/serhii.png"},
    {name: 'Prokop Veselý', role: 'Organizátor CS2 turnaje', discord: 'prokyss', instagram: 'prokyzz', category: 'tournaments', avatarUrl: "https://cloud02.emsio.cz/public/avatars/proky.webp" },
    //{name: 'Jáchym Klír', role: 'Organizátor CS2 turnaje', instagram: '@klirakk', category: 'tournaments', avatarUrl: "https://cloud02.emsio.cz/public/avatars/DSC_4222.jpg"},
    //{name: 'Sebastien Prejza', role: 'Organizátor CS2 turnaje', instagram: null, discord: null, category: 'tournaments', avatarUrl: null},
]

function OrganizerCard({org}: { org: Organizer }) {
    const initials = org.name
        .split(' ')
        .map((part) => part[0])
        .slice(0, 2)
        .join('')

    return (
        <div className={`${shell.card} ${style.organizerCard}`}>
            <div className={style.avatar} aria-hidden={! org.avatarUrl}>
                {org.avatarUrl ? (
                    <img src={org.avatarUrl} alt={`Avatar: ${org.name}`} className={style.avatarImage}/>
                ) : (
                    <span>{initials}</span>
                )}
            </div>
            <h3 className={style.name}>{org.name}</h3>
            <p className={style.role}>{org.role}</p>

            <If condition={!isNullOrUndefined(org.instagram) || !isNullOrUndefined(org.discord) || !isNullOrUndefined(org.phone) } as="div" className={style.socials}>
                <If condition={!isNullOrUndefined(org.phone)} as="div" className={style.social}>
                    <div style={{ '--icon': `url(/icons/phone.svg)`} as React.CSSProperties} className={style.icon}/>
                    <span className={style.val}>{org.phone}</span>
                </If>

                <If condition={!isNullOrUndefined(org.discord)} as="div" className={style.social}>
                    <div style={{ '--icon': `url(/icons/discord.svg)`} as React.CSSProperties} className={style.icon}/>
                    <span className={style.val}>{org.discord}</span>
                </If>
                
                <If condition={!isNullOrUndefined(org.instagram)} as="div" className={style.social}>
                    <div style={{ '--icon': `url(/icons/instagram.svg)`} as React.CSSProperties} className={style.icon}/>
                    <span className={style.val}>{org.instagram}</span>
                </If>
            </If>
        </div>
    )
}

export default function() {
    const [search, setSearch] = useState('')
    const query = search.toLowerCase()
    const event = siteConfig.currentEvent

    const teachers = organizers.filter((o) => o.category === 'teacher')
    const admins = organizers.filter((o) => o.category === 'admin')
    const grillmasters = organizers.filter((o) => o.category === 'grillmaster')
    const tournaments = organizers.filter((o) => o.category === 'tournaments')

    const filterOrg = (list: Organizer[]) =>
        list.filter((o) => o.name.toLowerCase().includes(query) || o.role.toLowerCase().includes(query))

    const filteredTeachers = filterOrg(teachers)
    const filteredAdmins = filterOrg(admins)
    const filteredGrillmasters = filterOrg(grillmasters)
    const filteredTournamentsOrgs = filterOrg(tournaments);

    return (
        <>
            <div className={shell.page}>
                <div className={shell.pageHeader}>
                    <span className={shell.eyebrow}>Organizátoři</span>
                    <h1 className={shell.title}>Organizátoři akce</h1>
                    <p className={shell.description}>
                        Kontakty na učitele, správce systému a další organizátory {event.title}.
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
                        <div className={shell.search}>
                            <span className={shell.searchIcon} aria-hidden="true"/>
                            <input
                                type="text"
                                className={shell.input}
                                placeholder="Hledat organizátory..."
                                value={search}
                                onChange={(e) => setSearch(e.target.value)}
                            />
                        </div>

                        <section id="ucitele" className={shell.section}>
                            <h2 className={shell.sectionTitle}>
                                <span className={shell.sectionMark} aria-hidden="true"/>
                                Učitelé
                            </h2>
                            {filteredTeachers.length > 0 ? (
                                <div className={shell.grid2}>
                                    {filteredTeachers.map((org) => (
                                        <OrganizerCard key={org.name} org={org}/>
                                    ))}
                                </div>
                            ) : (
                                <p className={shell.empty}>Žádné výsledky.</p>
                            )}
                        </section>

                        <section id="spravci" className={shell.section}>
                            <h2 className={shell.sectionTitle}>
                                <span className={shell.sectionMark} aria-hidden="true"/>
                                Správci LAN Party systému
                            </h2>
                            {filteredAdmins.length > 0 ? (
                                <div className={shell.grid2}>
                                    {filteredAdmins.map((org) => (
                                        <OrganizerCard key={org.name} org={org}/>
                                    ))}
                                </div>
                            ) : (
                                <p className={shell.empty}>Žádné výsledky.</p>
                            )}
                        </section>


                        <section id="organizatoriturnaju" className={shell.section}>
                            <h2 className={shell.sectionTitle}>
                                <span className={shell.sectionMark} aria-hidden="true"/>
                                Organizátoři turnajů
                            </h2>
                            {filteredTournamentsOrgs.length > 0 ? (
                                <div className={shell.grid2}>
                                    {filteredTournamentsOrgs.map((org) => (
                                        <OrganizerCard key={org.name} org={org}/>
                                    ))}
                                </div>
                            ) : (
                                <p className={shell.empty}>Zatím zde nikdo není :)</p>
                            )}
                        </section>

                        <section id="grillmasteri" className={shell.section}>
                            <h2 className={shell.sectionTitle}>
                                <span className={shell.sectionMark} aria-hidden="true"/>
                                Grillmasteři
                            </h2>
                            {filteredGrillmasters.length > 0 ? (
                                <div className={shell.grid2}>
                                    {filteredGrillmasters.map((org) => (
                                        <OrganizerCard key={org.name} org={org}/>
                                    ))}
                                </div>
                            ) : (
                                <p className={shell.empty}>Grillmasteři budou k dispozici u grilu během akce.</p>
                            )}
                        </section>


                        <section id="kontakt" className={shell.section}>
                            <h2 className={shell.sectionTitle}><span className={shell.sectionMark} aria-hidden="true"/>Kontakt
                            </h2>
                            <div className={`${shell.card} ${shell.copyCard}`}>
                                <p>
                                    Pokud budete mít v průběhu akce nějaký problém (nebo budete mít hlad), neváhejte
                                    organizátory kontaktovat ať osobně, tak na Discordu.
                                </p>
                                <p>
                                    Pokud máte nějaké dotazy, napište na školní Discord, nebo přímo organizátorům.
                                </p>
                            </div>
                        </section>
                    </div>
                </div>
            </div>
        </>
    )
}
