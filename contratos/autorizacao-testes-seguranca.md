# CONTRATO DE AUTORIZAÇÃO PARA TESTES DE SEGURANÇA
### (Autorização de Teste de Intrusão / Auditoria de Segurança)

> **Fortify — Auditoria de Segurança**
> Documento a assinar **antes** do início de qualquer teste. Sem este documento assinado, nenhum teste é realizado.

---

## 1. Partes

**PRESTADOR (quem faz a auditoria):**
- Nome / Empresa: _______________________________________
- NIF: _______________________________________
- Morada: _______________________________________
- Email / Telefone: _______________________________________

**CLIENTE (dono do sistema a testar):**
- Nome / Empresa: _______________________________________
- NIF: _______________________________________
- Morada: _______________________________________
- Representante legal: _______________________________________
- Email / Telefone: _______________________________________

---

## 2. Declaração de Propriedade e Autoridade

O CLIENTE declara, sob compromisso de honra, que:

- É o **legítimo proprietário** dos sistemas, domínios e servidores indicados no Ponto 3, **ou** tem **autoridade legal** para autorizar a realização de testes de segurança sobre os mesmos;
- Caso os sistemas estejam alojados em servidores de terceiros (ex: AWS, OVH, Cloudflare, outro alojamento), o CLIENTE é responsável por obter quaisquer autorizações adicionais que esses fornecedores exijam;
- Todas as informações fornecidas são verdadeiras.

---

## 3. Âmbito dos Testes (Scope)

### 3.1. Alvos AUTORIZADOS (o Prestador PODE testar):
| Domínio / Subdomínio / IP | Observações |
|---|---|
| _______________________________________ | _______________ |
| _______________________________________ | _______________ |
| _______________________________________ | _______________ |

### 3.2. Alvos EXCLUÍDOS (o Prestador NÃO PODE testar):
| Domínio / Subdomínio / IP | Motivo |
|---|---|
| _______________________________________ | _______________ |
| _______________________________________ | _______________ |

> Tudo o que **não** estiver listado no ponto 3.1 está automaticamente **fora do âmbito** e não será testado.

---

## 4. Tipo de Testes Autorizados

Marcar com [X] o que é autorizado:

- [ ] **Análise passiva** (cabeçalhos HTTP, configuração SSL/TLS, ficheiros expostos, deteção de versões/CVEs)
- [ ] **Reconhecimento** (mapeamento de subdomínios, portas e serviços)
- [ ] **Testes ativos OWASP Top 10** (SQL Injection, XSS, falhas de autenticação, controlo de acessos, CSRF, etc.)
- [ ] **Testes de força de credenciais** (passwords fracas) — *sem bloquear contas*
- [ ] Outro: _______________________________________

**Testes EXPRESSAMENTE PROIBIDOS:**
- Ataques de **negação de serviço (DoS/DDoS)**;
- Qualquer ação que **apague, altere ou corrompa** dados do CLIENTE;
- **Engenharia social** a colaboradores (phishing, etc.), salvo acordo específico por escrito;
- Acesso, cópia ou divulgação de dados pessoais além do estritamente necessário para provar uma vulnerabilidade.

---

## 5. Ambiente e Janela Temporal

- Ambiente: [ ] Produção   [ ] Testes/Homologação
- Data e hora de **início**: ____ / ____ / ______ às ____:____
- Data e hora de **fim**: ____ / ____ / ______ às ____:____
- Os testes serão realizados apenas dentro desta janela.

**Contacto de emergência do CLIENTE** (para avisar se algo for afetado):
- Nome: _______________ | Telefone: _______________ (disponível durante os testes)

---

## 6. Confidencialidade

- O PRESTADOR compromete-se a tratar **todas** as informações e vulnerabilidades encontradas como **estritamente confidenciais**;
- Nenhuma vulnerabilidade será divulgada a terceiros nem publicada;
- O relatório final é entregue **apenas** ao CLIENTE;
- Os dados recolhidos durante os testes serão **eliminados** pelo PRESTADOR no prazo de ____ dias após a entrega do relatório (salvo obrigação legal de conservação).

---

## 7. Proteção de Dados (RGPD)

- Ambas as partes cumprem o Regulamento Geral de Proteção de Dados (RGPD) e a legislação nacional aplicável;
- Caso os testes impliquem acesso a dados pessoais, o PRESTADOR atua como **subcontratante** do CLIENTE, tratando esses dados apenas para a finalidade da auditoria e segundo as instruções do CLIENTE.

---

## 8. Responsabilidade e Limitações

- Os testes de segurança envolvem, por natureza, algum risco de impacto nos sistemas. O CLIENTE declara estar ciente disso e recomenda-se que tenha **cópias de segurança (backups) atualizadas** antes do início;
- O PRESTADOR compromete-se a agir com diligência profissional e a minimizar riscos, evitando testes destrutivos;
- O PRESTADOR **não se responsabiliza** por falhas pré-existentes nos sistemas nem por danos resultantes de instabilidades já existentes;
- A auditoria reflete o estado de segurança **à data dos testes** e não garante a inexistência de outras vulnerabilidades.

---

## 9. Entregáveis

O PRESTADOR entregará ao CLIENTE:
- Um **relatório de auditoria** com: vulnerabilidades encontradas, nível de gravidade, provas (evidências), impacto e **recomendações de correção**;
- Prazo de entrega: até ____ dias úteis após o fim dos testes;
- [ ] Reunião de apresentação dos resultados (opcional).

---

## 10. Valor e Pagamento

- Serviço contratado: _______________________________________
- Valor: ____________ € ( [ ] com IVA  [ ] isento, art.º ____ )
- Condições de pagamento: _______________________________________

---

## 11. Revogação

O CLIENTE pode revogar esta autorização a qualquer momento, por escrito. A partir da receção da revogação, o PRESTADOR cessa imediatamente todos os testes.

---

## 12. Lei Aplicável

Este contrato rege-se pela lei portuguesa. Para qualquer litígio, as partes elegem o foro da comarca de _______________, com renúncia a qualquer outro.

---

## 13. Assinaturas

As partes declaram ter lido e aceitado todas as condições acima.

**Local e data:** _______________________, ____ / ____ / ______

&nbsp;

**O PRESTADOR**

Assinatura: _______________________________________

Nome: _______________________________________

&nbsp;

**O CLIENTE**

Assinatura: _______________________________________

Nome: _______________________________________

Cargo (se empresa): _______________________________________

---

> ⚠️ **Nota importante:** este é um modelo de base, pensado para pequenas auditorias a PME. Para contratos de maior valor ou clientes exigentes, recomenda-se revisão por um advogado. Não substitui aconselhamento jurídico profissional.
