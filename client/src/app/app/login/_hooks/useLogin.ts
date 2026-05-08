import { toast } from "react-hot-toast";
import {redirect} from "next/navigation"
import {useAuth} from "@/app/app/_providers/AuthProvider";
import {AccountSchema} from "@/schemas/AccountSchema";

export default function useLogin() {
    const { setAccount } = useAuth();

    async function login(email: string, passwordPlain: string) {
        const promise = fetch("/api/v1/account/login", {
            method: "POST",
            body: JSON.stringify({
                email,
                passwordPlain,
            }),
            headers: {
                "Content-Type": "application/json",
            }
        });

        await toast.promise(promise, {
            loading: "Přihlašování...",
        })

        const res = await promise;

        if(!res.ok) {
            toast.error("Login failed: " + res.statusText);
            return;
        }

        const json = await res.json();
        const account = AccountSchema.safeParse(json);
        if(!account.success) {
            toast.error("Login failed: Invalid response");
            return;
        }

        toast.success("Login success");
        setAccount(account.data);
        redirect("/app");
    }

    return {
        login,
    }
}