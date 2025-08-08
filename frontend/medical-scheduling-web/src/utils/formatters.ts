import {
  format,
  parseISO,
  formatDistance,
  isToday,
  isTomorrow,
  isYesterday,
} from "date-fns";
import { ptBR } from "date-fns/locale";

export const formatDate = (date: string | Date) => {
  const parsedDate = typeof date === "string" ? parseISO(date) : date;
  return format(parsedDate, "dd/MM/yyyy");
};

export const formatDateTime = (date: string | Date) => {
  const parsedDate = typeof date === "string" ? parseISO(date) : date;
  return format(parsedDate, "dd/MM/yyyy 'às' HH:mm");
};

export const formatDateLong = (date: string | Date) => {
  const parsedDate = typeof date === "string" ? parseISO(date) : date;
  return format(parsedDate, "dd 'de' MMMM 'de' yyyy", { locale: ptBR });
};

export const formatRelativeDate = (date: string | Date) => {
  const parsedDate = typeof date === "string" ? parseISO(date) : date;

  if (isToday(parsedDate)) return "Hoje";
  if (isTomorrow(parsedDate)) return "Amanhã";
  if (isYesterday(parsedDate)) return "Ontem";

  return formatDistance(parsedDate, new Date(), {
    addSuffix: true,
    locale: ptBR,
  });
};

export const formatCurrency = (value: number) => {
  return new Intl.NumberFormat("pt-BR", {
    style: "currency",
    currency: "BRL",
  }).format(value);
};

export const formatPhone = (phone: string) => {
  const cleaned = phone.replace(/\D/g, "");
  const match = cleaned.match(/^(\d{2})(\d{5})(\d{4})$/);
  if (match) {
    return `(${match[1]}) ${match[2]}-${match[3]}`;
  }
  return phone;
};

export const formatCPF = (cpf: string) => {
  const cleaned = cpf.replace(/\D/g, "");
  const match = cleaned.match(/^(\d{3})(\d{3})(\d{3})(\d{2})$/);
  if (match) {
    return `${match[1]}.${match[2]}.${match[3]}-${match[4]}`;
  }
  return cpf;
};
