import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react'
import { api } from '@/services/api'
import Cookies from 'js-cookie'
import { useRouter } from 'next/router'

interface User {
  id: number
  name: string
  email: string
  role: string
}

interface AuthContextType {
  user: User | null
  loading: boolean
  login: (email: string, password: string) => Promise<void>
  logout: () => void
  register: (data: RegisterData) => Promise<void>
}

interface RegisterData {
  name: string
  email: string
  password: string
  role: string
}

const AuthContext = createContext<AuthContextType | undefined>(undefined)

export const AuthProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null)
  const [loading, setLoading] = useState(true)
  const router = useRouter()

  useEffect(() => {
    const loadUserFromCookies = async () => {
      const token = Cookies.get('token')
      if (token) {
        api.defaults.headers.Authorization = `Bearer ${token}`
        try {

          const userData = JSON.parse(atob(token.split('.')[1]))
          setUser({
            id: userData.nameid,
            name: userData.name,
            email: userData.email,
            role: userData.role
          })
        } catch (error) {
          console.error('Error loading user data:', error)
          Cookies.remove('token')
          delete api.defaults.headers.Authorization
        }
      }
      setLoading(false)
    }

    loadUserFromCookies()
  }, [])

  const login = async (email: string, password: string) => {
    try {
      const response = await api.post('/auth/login', { email, password })
      const { token, ...userData } = response.data

      if (token) {
        Cookies.set('token', token, { expires: 7 })
        api.defaults.headers.Authorization = `Bearer ${token}`
        setUser(userData)
      }
    } catch (error: unknown) {
      let errorMessage = 'Erro ao fazer login'
      if (error instanceof Error) {
        errorMessage = error.message
      }
      throw new Error(errorMessage)
    }
  }

  const register = async (data: RegisterData) => {
    try {
      const response = await api.post('/auth/register', data)
      const { token, ...userData } = response.data

      if (token) {
        Cookies.set('token', token, { expires: 7 })
        api.defaults.headers.Authorization = `Bearer ${token}`
        setUser(userData)
      }
    } catch (error: unknown) {
      let errorMessage = 'Erro ao cadastrar'
      if (error instanceof Error) {
        errorMessage = error.message
      }
      throw new Error(errorMessage)
    }
  }

  const logout = () => {
    Cookies.remove('token')
    delete api.defaults.headers.Authorization
    setUser(null)
    router.push('/login')
  }

  return (
    <AuthContext.Provider value={{ user, loading, login, logout, register }}>
      {children}
    </AuthContext.Provider>
  )
}

export const useAuth = () => {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider')
  }
  return context
}