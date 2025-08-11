import React from 'react'
import { useRouter } from 'next/router'
import { useAuth } from '@/contexts/AuthContext'
import {
  UserCircleIcon,
  CalendarIcon,
  ArrowRightOnRectangleIcon,
} from '@heroicons/react/24/outline'
import styles from './Layout.module.css'

export const Navbar: React.FC = () => {
  const { user, logout } = useAuth()
  const router = useRouter()

  const handleLogout = async () => {
    await logout()
    router.push('/login')
  }

  const navigateToDashboard = () => {
    if (user?.role === 'Patient') {
      router.push('/patient/dashboard')
    } else if (user?.role === 'Doctor') {
      router.push('/doctor/dashboard')
    }
  }

  return (
    <header className={styles.navbar}>
      <div className={styles.navbarContent}>
        <div>
          <span 
            className={styles.logo}
            onClick={() => router.push('/')}
          >
            Medical Scheduling
          </span>
        </div>
        <div className={styles.navLinks}>
          <button
            onClick={navigateToDashboard}
            className={styles.navLink}
          >
            <CalendarIcon className="h-5 w-5" />
            Dashboard
          </button>
          <div>
            <button className={styles.navLink}>
              <UserCircleIcon className="h-5 w-5" />
              {user?.name}
            </button>
          </div>
          <button
            onClick={handleLogout}
            className={styles.navLink}
          >
            <ArrowRightOnRectangleIcon className="h-5 w-5" />
            Sair
          </button>
        </div>
      </div>
    </header>
  )
}