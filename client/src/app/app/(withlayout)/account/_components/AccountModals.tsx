import {ModalDestructive, ModalInformative} from "@/components/ModalDialog";
import {AccountPageState} from "../_hooks/types";

export function AccountModals({state}: {state: AccountPageState}) {
    return <>
        <ModalInformative
            open={state.modal === "avatar-info"}
            title="Změna avataru"
            description="Avatar si můžeš změnit pouze tak, že propojíš svůj účet s nějakou platformou. Po propojení se avatar automaticky změní na ten, který máš na této platformě."
            confirmText="Rozumím"
            cancelText="Zavřít"
            onClose={() => state.setModal(null)}
            onConfirm={() => state.setModal(null)}
        />

        <ModalInformative
            open={state.modal === "banner-info"}
            title="Změna banneru"
            description="Banner nelze ručně změnit. Pokud ho nechceš zobrazovat, můžeš ho smazat a uložit změny."
            confirmText="Rozumím"
            cancelText="Zavřít"
            onClose={() => state.setModal(null)}
            onConfirm={() => state.setModal(null)}
        />

        <ModalDestructive
            open={state.modal === "remove-avatar"}
            title="Smazat avatar"
            description="Opravdu chceš smazat svůj avatar? Změna se projeví po uložení profilu."
            confirmText="Smazat"
            cancelText="Zrušit"
            onClose={() => state.setModal(null)}
            onConfirm={() => {
                state.setProfileDraft({...state.profileDraft, avatarUrl: null});
                state.setModal(null);
            }}
        />

        <ModalDestructive
            open={state.modal === "remove-banner"}
            title="Smazat banner"
            description="Opravdu chceš smazat svůj banner? Změna se projeví po uložení profilu."
            confirmText="Smazat"
            cancelText="Zrušit"
            onClose={() => state.setModal(null)}
            onConfirm={() => {
                state.setProfileDraft({...state.profileDraft, bannerUrl: null});
                state.setModal(null);
            }}
        />
    </>;
}
