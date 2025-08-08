export interface Appointment {
  id: number
  appointmentDate: string
  symptoms: string
  recommendedSpecialty: string
  patientName?: string
  doctorName?: string
  status: 'pending' | 'confirmed' | 'completed' | 'cancelled'
}

export interface CreateAppointmentDTO {
  appointmentDate: string
  symptoms: string
}