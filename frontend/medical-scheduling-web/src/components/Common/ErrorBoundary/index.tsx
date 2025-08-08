import React, { Component, ErrorInfo, ReactNode } from 'react'
import styles from './ErrorBoundary.module.css'

interface Props {
    children: ReactNode
    fallback?: ReactNode
}

interface State {
    hasError: boolean
    error: Error | null
}

export class ErrorBoundary extends Component<Props, State> {
    public state: State = {
        hasError: false,
        error: null,
    }

    public static getDerivedStateFromError(error: Error): State {
        return { hasError: true, error }
    }

    public componentDidCatch(error: Error, errorInfo: ErrorInfo) {
        console.error('Uncaught error:', error, errorInfo)
    }

    private handleReset = () => {
        this.setState({ hasError: false, error: null })
        window.location.href = '/'
    }

    public render() {
        if (this.state.hasError) {
            if (this.props.fallback) {
                return this.props.fallback
            }

            return (
                <div className={styles.container}>
                    <div className={styles.content}>
                        <div className={styles.icon}>
                            <svg
                                className="w-16 h-16 text-red-500"
                                fill="none"
                                stroke="currentColor"
                                viewBox="0 0 24 24"
                            >
                                <path
                                    strokeLinecap="round"
                                    strokeLinejoin="round"
                                    strokeWidth={2}
                                    d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"
                                />
                            </svg>
                        </div>
                        <h1 className={styles.title}>Oops! Algo deu errado</h1>
                        <p className={styles.message}>
                            {this.state.error?.message || 'Ocorreu um erro inesperado'}
                        </p>
                        {process.env.NODE_ENV === 'development' && (
                            <details className={styles.details}>
                                <summary>Detalhes do erro (desenvolvimento)</summary>
                                <pre>{this.state.error?.stack}</pre>
                            </details>
                        )}
                        <button onClick={this.handleReset} className={styles.button}>
                            Voltar ao início
                        </button>
                    </div>
                </div>
            )
        }

        return this.props.children
    }
}
