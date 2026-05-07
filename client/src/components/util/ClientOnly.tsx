'use client'

import {ReactNode, useEffect, useState} from 'react'

export default function ClientOnly({children, fallback = null}: { children: ReactNode, fallback?: ReactNode | null }) {
    const [hasMounted, setHasMounted] = useState(false)

    useEffect(() => {
        setHasMounted(true)
    }, [])

    if (! hasMounted) {
        return <>{fallback}</>;
    }

    return <>{children}</>
}