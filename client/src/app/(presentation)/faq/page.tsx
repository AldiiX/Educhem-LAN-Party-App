import {Accordion, AccordionItem} from '@/components/accordion'
import {siteConfig} from '@/data/site'
import shell from '../page-shell.module.scss'
import type {Metadata} from "next";
import {Header} from "@/components/header";
import {Footer} from "@/components/footer";

const event = siteConfig.currentEvent

export const metadata: Metadata = {
    title: 'FAQ',
    description: `Odpovědi na nejčastější otázky o ${event.title}. Všechny informace vycházejí z oficiálního info PDF.`,
}

const faqItems = [
    {
        question: 'Kolik stojí vstupné a jak zaplatit?',
        answer: `Vstupné je ${event.fee}. Zaplaťte převodem na účet ${event.bankAccount} se zprávou: ${event.paymentMessage}. Můžete také použít QR kód na stránce Rezervace. Termín platby: do ${event.paymentDeadline}. Platby přiřazujeme ručně, takže potvrzení může trvat až 2 pracovní dny.`
    },
    {
        question: 'Kdy akce probíhá?',
        answer: `${event.title} probíhá od ${event.startDate} ${event.startTime} do ${event.endDate} ${event.endTime}. Časy jsou orientační.`
    },
    {
        question: 'Musím si rezervovat místo?',
        answer: 'Pokud si neplánujete brát s sebou PC ani být na školním PC, nemusíte si místo rezervovat. Kvůli velkému počtu účastníků je ale ideální rezervovat si počítač nebo místo pro vlastní setup. Pořád ale vstup musíte zaplatit.'
    },
    {
        question: 'Mohu přijít a odejít kdykoliv?',
        answer: 'Ano, můžete přijít/odejít kdykoliv během akce. Odchod ale musíte dát vědět někomu z učitelů, ideálně napsat na školní Discord.'
    },
    {
        question: 'Mohu si přinést vlastní PC/setup?',
        answer: 'Ano, můžete si vzít vlastní setup. Vezměte si vlastní monitor, prodlužovák a veškeré věci potřebné pro chod počítače. Software včetně operačního systému musí být aktualizovaný.'
    },
    {
        question: 'Jak funguje stahování her?',
        answer: 'Kvůli přetížení sítě jsou nastavená opatření pro stahování her. Doporučujeme mít vlastní externí HDD/SSD s nainstalovanými hrami.'
    },
    {
        question: 'Bude nějaké jídlo?',
        answer: 'Ano, jídlo na grilování a pití je v ceně vstupného. Jídlo vždy vydává grillmaster a u grilu platí zásady slušného chování.'
    },
    {
        question: 'Může se moje rezervované místo změnit?',
        answer: 'Ano, může se stát, že vaše místo bude změněno kvůli úpravám rozložení. Často se spolužáci dávají vedle sebe nebo do stejných tříd.'
    },
    {
        question: 'Na koho se obrátit s problémy?',
        answer: 'Kontaktujte organizátory osobně nebo na školním Discordu. Problémy se systémem řeší správci: Stanislav Škudrna (@aldiix) a Serhii Yavorskyi (@_.yavorskiy.s._).'
    },
    {
        question: 'Mohu používat školní periferie?',
        answer: 'Školní PC můžete používat, ale není dovoleno měnit jejich zapojení, odpojovat monitory ani měnit periferie včetně myší a klávesnice.'
    },
    {
        question: 'Platí noční klid?',
        answer: 'V noci snižte hlasitost, abyste minimalizovali rušení okolního prostředí během nočního klidu.'
    },
]

export default function() {
    return (
        <>
            <div className={`${shell.page} ${shell.narrow}`}>
                <div className={shell.pageHeader}>
                    <span className={shell.eyebrow}>FAQ</span>
                    <h1 className={shell.title}>Často kladené otázky</h1>
                    <p className={shell.description}>
                        Odpovědi na nejčastější otázky o {event.title}. Všechny informace vycházejí z oficiálního
                        info PDF.
                    </p>
                </div>

                <Accordion>
                    {faqItems.map((item, idx) => (
                        <AccordionItem key={idx} title={item.question}>
                            <p>{item.answer}</p>
                        </AccordionItem>
                    ))}
                </Accordion>
            </div>
        </>
    )
}
