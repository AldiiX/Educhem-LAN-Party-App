import {useCallback} from "react";
import {useRememberState} from "@/hooks/useRememberState";

export function useRememberedCollapseState(initialValue: boolean, key: string) {
    const [isCollapsed, setIsCollapsed] = useRememberState(key, initialValue);

    const toggle = useCallback(() => {
        setIsCollapsed(currentValue => !currentValue);
    }, [setIsCollapsed]);

    return [isCollapsed, toggle] as const;
}
