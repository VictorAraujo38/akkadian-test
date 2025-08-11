import axios from "axios";
import Cookies from "js-cookie";

const baseURL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";

export const api = axios.create({
  baseURL,
  headers: {
    "Content-Type": "application/json",
  },
});

// Interceptor para adicionar o token em todas as requisições
api.interceptors.request.use(
  (config) => {
    const token = Cookies.get("token");
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Interceptor para tratamento de erros
api.interceptors.response.use(
  (response) => response,
  (error) => {
    // Se o erro for 401 (Unauthorized), redireciona para o login
    if (error.response && error.response.status === 401) {
      Cookies.remove("token");
      if (typeof window !== "undefined") {
        window.location.href = "/login";
      }
    }
    return Promise.reject(error);
  }
);

export const appointmentService = {
  getTriageRecommendation: async (symptoms: string) => {
    const response = await api.post("/mock/triagem", { symptoms });
    return response.data;
  },

  createAppointment: async (data: Record<string, unknown>) => {
    const response = await api.post("/paciente/agendamentos", data);
    return response.data;
  },

  getPatientAppointments: async () => {
    const response = await api.get("/paciente/agendamentos");
    return response.data;
  },

  getDoctorAppointments: async (date: Date) => {
    const dateStr = date.toISOString().split("T")[0];
    const response = await api.get(`/medico/agendamentos?data=${dateStr}`);
    return response.data;
  },

  // Novo método para buscar horários disponíveis
  getAvailableTimeSlots: async (date: string) => {
    const response = await api.get(
      `/appointments/available-slots?date=${date}`
    );
    return response.data;
  },

  // Novo método para validar agendamento
  validateAppointment: async (data: {
    patientId: number;
    appointmentDate: string;
    doctorId?: number;
  }) => {
    const response = await api.post("/appointments/validate", data);
    return response.data;
  },

  // Novo método para buscar médicos por especialidade
  getDoctorsBySpecialty: async (specialty: string, date: string) => {
    const response = await api.get(
      `/appointments/doctors-by-specialty?specialty=${specialty}&date=${date}`
    );
    return response.data;
  },
};

export const doctorService = {
  getAppointmentsByDate: async (date: Date) => {
    const dateStr = date.toISOString().split("T")[0];
    const response = await api.get(`/medico/agendamentos?data=${dateStr}`);
    return response.data;
  },

  updateAppointmentStatus: async (appointmentId: number, status: string) => {
    const response = await api.patch(`/appointments/${appointmentId}/status`, {
      status,
    });
    return response.data;
  },

  getScheduleOverview: async (startDate: Date, endDate: Date) => {
    // Como não temos esse endpoint no backend, vamos usar o endpoint existente
    const appointments = [];
    const currentDate = new Date(startDate);
    
    while (currentDate <= endDate) {
      try {
        const dayAppointments = await api.get(`/medico/agendamentos?data=${currentDate.toISOString().split('T')[0]}`);
        appointments.push(...dayAppointments.data);
      } catch (error) {
        console.warn(`Erro ao buscar agendamentos para ${currentDate.toISOString().split('T')[0]}:`, error);
      }
      currentDate.setDate(currentDate.getDate() + 1);
    }
    
    return appointments;
  },
};

export const patientService = {
  getAppointments: async () => {
    const response = await api.get("/paciente/agendamentos");
    return response.data;
  },

  cancelAppointment: async (appointmentId: number) => {
    const response = await api.patch(`/appointments/${appointmentId}/cancel`);
    return response.data;
  },

  rescheduleAppointment: async (appointmentId: number, newDate: string, reason?: string) => {
    const response = await api.patch(
      `/appointments/${appointmentId}/reschedule`,
      {
        newAppointmentDate: newDate,
        reason: reason || 'Reagendamento solicitado pelo paciente'
      }
    );
    return response.data;
  },
};

export const triageService = {
  analyzeSymptoms: async (symptoms: string) => {
    const response = await api.post("/mock/triagem", { symptoms });
    return response.data;
  },

  getSpecialtyInfo: async (specialty: string) => {
    // Como não temos endpoint específico para specialty info, vamos buscar todas as especialidades
    const response = await api.get('/specialties');
    const specialties = response.data;
    return specialties.find((s: any) => s.name.toLowerCase() === specialty.toLowerCase()) || null;
  },
};
