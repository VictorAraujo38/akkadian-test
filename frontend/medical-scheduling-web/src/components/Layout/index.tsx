import React, { ReactNode } from 'react'
import Head from 'next/head'
import { useRouter } from 'next/router'
import { useAuth } from '@/contexts/AuthContext'
import { LoadingSpinner } from '@/components/Common/LoadingSpinner'
import { Navbar } from './Navbar'
import { Footer } from './Footer'
import styles from './Layout.module.css'

interface LayoutProps {
  children: ReactNode
  title?: string
  description?: string
}

const Layout: React.FC<LayoutProps> = ({ 
  children, 
  title = 'Medical Scheduling', 
  description = 'Sistema de agendamento médico com triagem por IA' 
}) => {
  const { user, loading } = useAuth()
  const router = useRouter()

  if (loading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-blue-50 to-indigo-100">
        <LoadingSpinner size="large" />
      </div>
    )
  }

  if (!user) {
    router.push('/login')
    return null
  }

  return (
    <>
      <Head>
        <title>{title}</title>
        <meta name="description" content={description} />
      </Head>
      <div className={styles.container}>
        <Navbar />
        <main className={styles.main}>
          <div className={styles.content}>
            {children}
          </div>
        </main>
        <Footer />
      </div>
    </>
  )
}

export default Layout