// E-mails com acesso admin (espelha Admin:Emails do appsettings do backend). O JWT atual NÃO
// carrega claim de role, então o gate client-side é por e-mail (apenas p/ mostrar o link/página;
// o gate REAL é a policy "Admin" no backend, que rejeita com 403 quem não é admin).
const ADMIN_EMAILS = ["rodneydocarmo@gmail.com"];

export function isAdminEmail(email?: string | null): boolean {
  if (!email) return false;
  return ADMIN_EMAILS.includes(email.trim().toLowerCase());
}
