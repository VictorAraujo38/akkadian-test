import { useState, useEffect } from 'react'
import { api } from '@/services/api'
import { toast } from 'react-toastify'

export const usePatientDashboard = () => {
  const [appointments, setAppointments] = useState([])
  const [loading, setLoading] = useState(true)
  const [filter, setFilter] = useState<'all' | 'upcoming' | 'past'>('all')

  useEffect(() => {
    fetchAppointments()
  }, [])

  const fetchAppointments = async () => {
    try {
      const response = await api.get('/paciente/agendamentos')
      setAppointments(response.data)
    } catch (error) {
      toast.error('Erro ao carregar agendamentos')
    } finally {
      setLoading(false)
    }
  }

  const stats = {
    total: appointments.length,
    upcoming: appointments.filter((a: any) => new Date(a.appointmentDate) >= new Date()).length,
    completed: appointments.filter((a: any) => new Date(a.appointmentDate) < new Date()).length
  }

  return {
    appointments,
    loading,
    filter,
    setFilter,
    stats,
    refetch: fetchAppointments
  }
}