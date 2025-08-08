import React, { createContext, useContext, useState, ReactNode } from 'react'
import { LoadingSpinner } from '@/components/Common/LoadingSpinner'

interface LoadingContextType {
    isLoading: boolean
    setLoading: (loading: boolean) => void
    showLoading: () => void
    hideLoading: () => void
}

const LoadingContext = createContext<LoadingContextType | undefined>(undefined)

export const LoadingProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
    const [isLoading, setIsLoading] = useState(false)

    const showLoading = () => setIsLoading(true)
    const hideLoading = () => setIsLoading(false)
    const setLoading = (loading: boolean) => setIsLoading(loading)

    return (
        <LoadingContext.Provider value={{ isLoading, setLoading, showLoading, hideLoading }}>
            {children}
            {isLoading && <LoadingSpinner fullScreen size="large" />}
        </LoadingContext.Provider>
    )
}

export const useLoading = () => {
    const context = useContext(LoadingContext)
    if (!context) {
        throw new Error('useLoading must be used within LoadingProvider')
    }
    return context
}
