-- Fortify — esquema da base de dados (Supabase / Postgres)
-- Aplicado ao projeto via migrações. Mantido aqui para referência e versionamento.

-- Perfis de utilizador (liga-se a auth.users)
create table if not exists public.profiles (
  id uuid primary key references auth.users(id) on delete cascade,
  name text,
  is_admin boolean not null default false,
  created_at timestamptz not null default now()
);

alter table public.profiles enable row level security;

create policy "profiles_select_own" on public.profiles
  for select using (auth.uid() = id);
create policy "profiles_update_own" on public.profiles
  for update using (auth.uid() = id);

-- Função auxiliar: o utilizador atual é admin?
create or replace function public.is_admin()
returns boolean language sql security definer stable set search_path = public as $$
  select coalesce((select is_admin from public.profiles where id = auth.uid()), false);
$$;

create policy "profiles_admin_select_all" on public.profiles
  for select using (public.is_admin());

-- Cria perfil automaticamente ao registar
create or replace function public.handle_new_user()
returns trigger language plpgsql security definer set search_path = public as $$
begin
  insert into public.profiles (id, name)
  values (new.id, coalesce(new.raw_user_meta_data->>'name', ''));
  return new;
end;
$$;

-- A função de trigger não deve ser chamável via API REST
revoke execute on function public.handle_new_user() from anon, authenticated, public;

drop trigger if exists on_auth_user_created on auth.users;
create trigger on_auth_user_created
  after insert on auth.users
  for each row execute function public.handle_new_user();

-- Contratos
create table if not exists public.contracts (
  id uuid primary key default gen_random_uuid(),
  user_id uuid not null references auth.users(id) on delete cascade,
  empresa text not null,
  nif text,
  representante text not null,
  site text not null,
  contacto text,
  verification_method text,
  verification_token text not null,
  verified boolean not null default false,
  verified_at timestamptz,
  signed boolean not null default false,
  signature text,
  signed_at timestamptz,
  created_at timestamptz not null default now()
);

alter table public.contracts enable row level security;

create policy "contracts_select_own" on public.contracts
  for select using (auth.uid() = user_id);
create policy "contracts_insert_own" on public.contracts
  for insert with check (auth.uid() = user_id);
create policy "contracts_update_own" on public.contracts
  for update using (auth.uid() = user_id);

create policy "contracts_admin_select_all" on public.contracts
  for select using (public.is_admin());

create index if not exists contracts_user_idx on public.contracts(user_id);
create index if not exists contracts_created_idx on public.contracts(created_at desc);

-- Para tornar um utilizador admin (correr uma vez, com o email do dono):
-- update public.profiles set is_admin = true
-- where id = (select id from auth.users where email = 'o-teu-email@exemplo.pt');
