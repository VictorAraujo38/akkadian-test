import React from 'react'
import { format } from 'date-fns'
import { ptBR } from 'date-fns/locale'

interface Patient {
  id: string
  name: string
  email: string
  phone: string
}

interface Appointment {
  id: string
  patientId: string
  patient: Patient
  doctorId: string
  date: string
  time: string
  status: 'scheduled' | 'completed' | 'cancelled'
  notes?: string
}

interface PatientListProps {
  appointments: Appointment[]
  selectedDate: Date
  loading: boolean
}

export const PatientList: React.FC<PatientListProps> = ({ 
  appointments, 
  selectedDate,
  loading 
}) => {
  // Filter appointments for the selected date
  const filteredAppointments = appointments.filter(appointment => {
    const appointmentDate = new Date(appointment.date)
    return (
      appointmentDate.getDate() === selectedDate.getDate() &&
      appointmentDate.getMonth() === selectedDate.getMonth() &&
      appointmentDate.getFullYear() === selectedDate.getFullYear()
    )
  })

  // Sort appointments by time
  const sortedAppointments = [...filteredAppointments].sort((a, b) => {
    return a.time.localeCompare(b.time)
  })

  if (loading) {
    return (
      <div className="bg-white rounded-lg shadow p-4 h-full">
        <h2 className="text-lg font-semibold mb-4">Pacientes do Dia</h2>
        <div className="flex justify-center items-center h-64">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-500"></div>
        </div>
      </div>
    )
  }

  return (
    <div className="bg-white rounded-lg shadow p-4 h-full">
      <h2 className="text-lg font-semibold mb-4">Pacientes do Dia</h2>
      <div className="text-sm text-gray-500 mb-4">
        {format(selectedDate, "EEEE, d 'de' MMMM 'de' yyyy", { locale: ptBR })}
      </div>

      {sortedAppointments.length === 0 ? (
        <div className="text-center py-8 text-gray-500">
          Não há consultas agendadas para esta data.
        </div>
      ) : (
        <div className="space-y-4">
          {sortedAppointments.map((appointment) => (
            <div key={appointment.id} className="border-l-4 border-blue-500 pl-3 py-2">
              <div className="font-medium">{appointment.patient.name}</div>
              <div className="text-sm text-gray-500">{appointment.time}</div>
              <div className="flex mt-2 space-x-2">
                <span 
                  className={`px-2 py-1 text-xs rounded-full ${
                    appointment.status === 'scheduled' ? 'bg-yellow-100 text-yellow-800' :
                    appointment.status === 'completed' ? 'bg-green-100 text-green-800' :
                    'bg-red-100 text-red-800'
                  }`}
                >
                  {appointment.status === 'scheduled' ? 'Agendado' :
                   appointment.status === 'completed' ? 'Concluído' :
                   'Cancelado'}
                </span>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}