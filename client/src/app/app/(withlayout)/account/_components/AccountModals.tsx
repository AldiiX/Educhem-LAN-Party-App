import {ModalDestructive, ModalError, ModalInformative} from "@/components/ModalDialog";
import {phrase} from "@/lib/communicationStyle";
import {AccountPageState} from "../_hooks/types";

export function AccountModals({state}: {state: AccountPageState}) {
    const communicationStyle = state.account.communicationStyle;

    return <>
        <ModalError
            open={state.modal === "platform-error"}
            title="Propojení se nezdařilo"
            description={state.platformErrorMessage ?? "Účet se nepodařilo propojit."}
            confirmText="Zavřít"
            onClose={() => state.setModal(null)}
            onConfirm={() => state.setModal(null)}
        />

        <ModalInformative
            open={state.modal === "avatar-info"}
            title="Změna avataru"
            description={phrase(
                communicationStyle,
                "Avatar si můžeš změnit pouze tak, že propojíš svůj účet s nějakou platformou. Po propojení se avatar automaticky změní na ten, který máš na této platformě.",
                "Avatar si můžete změnit pouze tak, že propojíte svůj účet s nějakou platformou. Po propojení se avatar automaticky změní na ten, který máte na této platformě."
            )}
            confirmText="Rozumím"
            cancelText="Zavřít"
            onClose={() => state.setModal(null)}
            onConfirm={() => state.setModal(null)}
        />

        <ModalInformative
            open={state.modal === "banner-info"}
            title="Změna banneru"
            description={phrase(
                communicationStyle,
                "Banner nelze ručně změnit. Pokud ho nechceš zobrazovat, můžeš ho smazat a uložit změny.",
                "Banner nelze ručně změnit. Pokud ho nechcete zobrazovat, můžete ho smazat a uložit změny."
            )}
            confirmText="Rozumím"
            cancelText="Zavřít"
            onClose={() => state.setModal(null)}
            onConfirm={() => state.setModal(null)}
        />

        <ModalDestructive
            open={state.modal === "remove-avatar"}
            title="Smazat avatar"
            description={phrase(
                communicationStyle,
                "Opravdu chceš smazat svůj avatar? Změna se projeví po uložení profilu.",
                "Opravdu chcete smazat svůj avatar? Změna se projeví po uložení profilu."
            )}
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
            description={phrase(
                communicationStyle,
                "Opravdu chceš smazat svůj banner? Změna se projeví po uložení profilu.",
                "Opravdu chcete smazat svůj banner? Změna se projeví po uložení profilu."
            )}
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
