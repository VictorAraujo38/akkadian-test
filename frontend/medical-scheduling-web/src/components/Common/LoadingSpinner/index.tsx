import React from 'react'
import styles from './LoadingSpinner.module.css'

interface LoadingSpinnerProps {
    size?: 'small' | 'medium' | 'large'
    color?: string
    fullScreen?: boolean
}

export const LoadingSpinner: React.FC<LoadingSpinnerProps> = ({
    size = 'medium',
    color = '#3b82f6',
    fullScreen = false,
}) => {
    const spinnerClass = `${styles.spinner} ${styles[size]}`

    if (fullScreen) {
        return (
            <div className={styles.fullScreenContainer}>
                <div className={spinnerClass} style={{ borderTopColor: color }} />
            </div>
        )
    }

    return <div className={spinnerClass} style={{ borderTopColor: color }} />
}