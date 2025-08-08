import React from 'react'
import { format, addMonths, subMonths, startOfMonth, endOfMonth, eachDayOfInterval, isSameMonth, isSameDay, isToday } from 'date-fns'
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

type ViewMode = 'month' | 'week' | 'day'

interface AppointmentCalendarProps {
  appointments: Appointment[]
  selectedDate: Date
  onDateChange: (date: Date) => void
  viewMode: ViewMode
  onViewModeChange: (mode: ViewMode) => void
  loading: boolean
}

export const AppointmentCalendar: React.FC<AppointmentCalendarProps> = ({
  appointments,
  selectedDate,
  onDateChange,
  viewMode,
  onViewModeChange,
  loading
}) => {
  const handlePrevMonth = () => {
    onDateChange(subMonths(selectedDate, 1))
  }

  const handleNextMonth = () => {
    onDateChange(addMonths(selectedDate, 1))
  }

  const handleDayClick = (day: Date) => {
    onDateChange(day)
  }

  // Generate days for the current month view
  const monthStart = startOfMonth(selectedDate)
  const monthEnd = endOfMonth(selectedDate)
  const daysInMonth = eachDayOfInterval({ start: monthStart, end: monthEnd })

  // Get appointments for each day
  const getAppointmentsForDay = (day: Date) => {
    return appointments.filter(appointment => {
      const appointmentDate = new Date(appointment.date)
      return isSameDay(appointmentDate, day)
    })
  }

  if (loading) {
    return (
      <div className="bg-white rounded-lg shadow p-4 h-full">
        <div className="flex justify-between items-center mb-4">
          <h2 className="text-lg font-semibold">Calendário de Consultas</h2>
          <div className="flex space-x-2">
            <button className="px-3 py-1 text-sm bg-gray-100 rounded-md">Mês</button>
            <button className="px-3 py-1 text-sm bg-gray-100 rounded-md">Semana</button>
            <button className="px-3 py-1 text-sm bg-gray-100 rounded-md">Dia</button>
          </div>
        </div>
        <div className="flex justify-center items-center h-64">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-500"></div>
        </div>
      </div>
    )
  }

  return (
    <div className="bg-white rounded-lg shadow p-4 h-full">
      <div className="flex justify-between items-center mb-4">
        <h2 className="text-lg font-semibold">Calendário de Consultas</h2>
        <div className="flex space-x-2">
          <button 
            className={`px-3 py-1 text-sm rounded-md ${viewMode === 'month' ? 'bg-blue-500 text-white' : 'bg-gray-100'}`}
            onClick={() => onViewModeChange('month')}
          >
            Mês
          </button>
          <button 
            className={`px-3 py-1 text-sm rounded-md ${viewMode === 'week' ? 'bg-blue-500 text-white' : 'bg-gray-100'}`}
            onClick={() => onViewModeChange('week')}
          >
            Semana
          </button>
          <button 
            className={`px-3 py-1 text-sm rounded-md ${viewMode === 'day' ? 'bg-blue-500 text-white' : 'bg-gray-100'}`}
            onClick={() => onViewModeChange('day')}
          >
            Dia
          </button>
        </div>
      </div>

      <div className="mb-4 flex justify-between items-center">
        <button 
          onClick={handlePrevMonth}
          className="p-2 rounded-full hover:bg-gray-100"
        >
          <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
            <path fillRule="evenodd" d="M12.707 5.293a1 1 0 010 1.414L9.414 10l3.293 3.293a1 1 0 01-1.414 1.414l-4-4a1 1 0 010-1.414l4-4a1 1 0 011.414 0z" clipRule="evenodd" />
          </svg>
        </button>
        <h3 className="text-lg font-medium">
          {format(selectedDate, 'MMMM yyyy', { locale: ptBR })}
        </h3>
        <button 
          onClick={handleNextMonth}
          className="p-2 rounded-full hover:bg-gray-100"
        >
          <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
            <path fillRule="evenodd" d="M7.293 14.707a1 1 0 010-1.414L10.586 10 7.293 6.707a1 1 0 011.414-1.414l4 4a1 1 0 010 1.414l-4 4a1 1 0 01-1.414 0z" clipRule="evenodd" />
          </svg>
        </button>
      </div>

      <div className="grid grid-cols-7 gap-1">
        {['Dom', 'Seg', 'Ter', 'Qua', 'Qui', 'Sex', 'Sáb'].map((day) => (
          <div key={day} className="text-center text-sm font-medium py-2">
            {day}
          </div>
        ))}

        {Array(monthStart.getDay())
          .fill(null)
          .map((_, index) => (
            <div key={`empty-${index}`} className="h-12 p-1"></div>
          ))}

        {daysInMonth.map((day) => {
          const dayAppointments = getAppointmentsForDay(day)
          const isSelected = isSameDay(day, selectedDate)
          const isTodayDate = isToday(day)

          return (
            <div 
              key={day.toString()} 
              className={`h-12 p-1 relative cursor-pointer ${isSelected ? 'bg-blue-100 rounded' : ''}`}
              onClick={() => handleDayClick(day)}
            >
              <div 
                className={`flex justify-center items-center h-6 w-6 ${isTodayDate ? 'bg-blue-500 text-white rounded-full' : ''}`}
              >
                {format(day, 'd')}
              </div>
              {dayAppointments.length > 0 && (
                <div className="absolute bottom-1 right-1 h-2 w-2 bg-blue-500 rounded-full"></div>
              )}
            </div>
          )
        })}
      </div>

      {viewMode === 'day' && (
        <div className="mt-4 border-t pt-4">
          <h4 className="font-medium mb-2">
            Consultas para {format(selectedDate, "d 'de' MMMM", { locale: ptBR })}
          </h4>
          <div className="space-y-2">
            {getAppointmentsForDay(selectedDate).length === 0 ? (
              <p className="text-gray-500 text-sm">Não há consultas agendadas para este dia.</p>
            ) : (
              getAppointmentsForDay(selectedDate).map((appointment) => (
                <div key={appointment.id} className="flex items-center p-2 bg-gray-50 rounded">
                  <div className="mr-2 text-sm font-medium">{appointment.time}</div>
                  <div className="flex-1">
                    <div className="font-medium">{appointment.patient.name}</div>
                    <div className="text-xs text-gray-500">{appointment.patient.phone}</div>
                  </div>
                  <div>
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
              ))
            )}
          </div>
        </div>
      )}
    </div>
  )
}