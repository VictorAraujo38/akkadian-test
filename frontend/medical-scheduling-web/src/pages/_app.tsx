import '@/styles/globals.css'
import type { AppProps } from 'next/app'
import { AuthProvider } from '@/contexts/AuthContext'
import { ToastContainer } from 'react-toastify'
import 'react-toastify/dist/ReactToastify.css'
import { ErrorBoundary } from '@/components/Common/ErrorBoundary'
import { LoadingProvider } from '@/contexts/LoadingContext'
import NProgress from 'nprogress'
import 'nprogress/nprogress.css'
import { useEffect } from 'react'
import { useRouter } from 'next/router'

NProgress.configure({ showSpinner: false })

export default function App({ Component, pageProps }: AppProps) {
    const router = useRouter()

    useEffect(() => {
        const handleStart = () => NProgress.start()
        const handleComplete = () => NProgress.done()

        router.events.on('routeChangeStart', handleStart)
        router.events.on('routeChangeComplete', handleComplete)
        router.events.on('routeChangeError', handleComplete)

        return () => {
            router.events.off('routeChangeStart', handleStart)
            router.events.off('routeChangeComplete', handleComplete)
            router.events.off('routeChangeError', handleComplete)
        }
    }, [router])

    return (
        <ErrorBoundary>
            <LoadingProvider>
                <AuthProvider>
                    <Component {...pageProps} />
                    <ToastContainer
                        position="top-right"
                        autoClose={5000}
                        hideProgressBar={false}
                        newestOnTop
                        closeOnClick
                        rtl={false}
                        pauseOnFocusLoss
                        draggable
                        pauseOnHover
                        theme="light"
                    />
                </AuthProvider>
            </LoadingProvider>
        </ErrorBoundary>
    )
}