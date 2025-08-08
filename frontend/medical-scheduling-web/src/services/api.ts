import axios from 'axios'
import Cookies from 'js-cookie'

const baseURL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000'

export const api = axios.create({
  baseURL,
  headers: {
    'Content-Type': 'application/json'
  }
})

// Interceptor para adicionar o token em todas as requisições
api.interceptors.request.use(
  (config) => {
    const token = Cookies.get('token')
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  },
  (error) => Promise.reject(error)
)

// Interceptor para tratamento de erros
api.interceptors.response.use(
  (response) => response,
  (error) => {
    // Se o erro for 401 (Unauthorized), redireciona para o login
    if (error.response && error.response.status === 401) {
      Cookies.remove('token')
      if (typeof window !== 'undefined') {
        window.location.href = '/login'
      }
    }
    return Promise.reject(error)
  }
)

export const appointmentService = {
  getTriageRecommendation: async (symptoms: string) => {
    const response = await api.post('/triagem', { symptoms })
    return response.data
  },
  
  createAppointment: async (data: Record<string, unknown>) => {
    const response = await api.post('/paciente/agendamentos', data)
    return response.data
  },
  
  getPatientAppointments: async () => {
    const response = await api.get('/paciente/agendamentos')
    return response.data
  },
  
  getDoctorAppointments: async (date: Date) => {
    const dateStr = date.toISOString().split('T')[0]
    const response = await api.get(`/medico/agendamentos?data=${dateStr}`)
    return response.data
  }
}
