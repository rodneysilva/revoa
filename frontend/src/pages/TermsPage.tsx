export function TermsPage() {
  return (
    <div className="app-container app-read">
      <h1 className="text-3xl font-bold text-cream mb-2">Termos de Uso</h1>
      <p className="text-silver mb-8">Última atualização: agosto de 2026 · Leia com atenção.</p>

      <div className="space-y-8 text-cream/90 leading-relaxed">
        <section>
          <h2 className="text-xl font-bold text-esmeralda mb-2">O que é o revoa.me</h2>
          <p>
            O revoa.me é uma plataforma <strong>sem fins lucrativos</strong> de economia circular e
            ajuda mútua. Usamos uma moeda social chamada <strong>RVM (crédito de troca)</strong> para
            facilitar trocas, doações e voluntariado entre vizinhos. <strong>O RVM não é moeda,
            criptomoeda, ativo financeiro nem investimento.</strong> É um crédito de troca da
            comunidade — meio, não fim.
          </p>
        </section>

        <section>
          <h2 className="text-xl font-bold text-esmeralda mb-2">Como funciona (resumo)</h2>
          <ul className="list-disc pl-5 space-y-1 text-sm">
            <li><strong>Trocar:</strong> ofereça o que não usa mais (produto ou serviço) e receba créditos RVM.</li>
            <li><strong>Doar:</strong> doe de graça — quem precisa pede e você escolhe quem recebe.</li>
            <li><strong>Voluntariar:</strong> ofereça seu tempo/talento — ganhe reconhecimento da comunidade.</li>
            <li><strong>Repassar:</strong> venda baratinho (poucos créditos) o que não usa mais.</li>
          </ul>
          <p className="mt-2 text-sm text-silver">
            Cada anúncio mostra um valor em RVM e uma <strong>estimativa em R$ (simbólica)</strong>{" "}
            apenas para dar noção — não é um preço real nem câmbio.
          </p>
        </section>

        <section>
          <h2 className="text-xl font-bold text-esmeralda mb-2">RVM — crédito de troca</h2>
          <ul className="list-disc pl-5 space-y-1 text-sm">
            <li>Você recebe <strong>créditos gratuitos</strong> ao se cadastrar (equivalente simbólico a R$20).</li>
            <li>Nas trocas, uma pequena taxa (2%) vai para o <strong>Fundo Comunitário</strong> (mantém a plataforma no ar — sem lucro).</li>
            <li>Créditos parados por muito tempo perdem um pouquinho de valor (<strong>demurrage</strong>) — isso incentiva a circular em vez de acumular. Há um piso de isenção (R$100 equivalente) que protege pequenos saldos.</li>
            <li>O valor dos créditos é <strong>definido pela própria comunidade</strong> (mediana dos anúncios), com ajuste periódico pela inflação (IPCA).</li>
          </ul>
          <p className="mt-2 text-sm text-silver">
            Detalhes técnicos completos em <a href="/transparency" className="text-sky hover:underline">Transparência</a>.
          </p>
        </section>

        <section>
          <h2 className="text-xl font-bold text-esmeralda mb-2">Sua responsabilidade</h2>
          <ul className="list-disc pl-5 space-y-1 text-sm">
            <li>Você é responsável pelos itens que anuncia e pelas combinações que faz (entrega, retirada).</li>
            <li>A plataforma conecta pessoas — <strong>não intermedeiamos pagamentos</strong> em dinheiro real.</li>
            <li>Seja honesto nas descrições. A comunidade avalia (estrelas) após cada troca.</li>
            <li>Denúncias (spam, golpes, conteúdo inadequado) são analisadas pela moderação.</li>
          </ul>
        </section>

        <section>
          <h2 className="text-xl font-bold text-esmeralda mb-2">Privacidade</h2>
          <ul className="list-disc pl-5 space-y-1 text-sm">
            <li>Seus dados (nome, e-mail, telefone) são usados apenas para o funcionamento da plataforma.</li>
            <li>Não vendemos nem compartilhamos seus dados com terceiros.</li>
            <li>Você pode solicitar a exclusão da sua conta a qualquer momento.</li>
          </ul>
        </section>

        <section>
          <h2 className="text-xl font-bold text-amber mb-2">Importante</h2>
          <div className="bg-charcoal/60 border border-smoke rounded-xl p-4 text-sm space-y-2">
            <p>
              <strong>O RVM não é um investimento.</strong> Não prometemos valorização, rendimento nem
              retorno. O valor pode flutuar conforme o uso da comunidade.
            </p>
            <p>
              <strong>O revoa.me é um projeto informal (sem CNPJ)</strong> em fase de teste (MVP).
              A formalização como associação/OSC acontecerá antes da abertura ao público.
            </p>
            <p>
              Participar é <strong>gratuito e voluntário</strong>. Você é responsável por suas ações
              na plataforma.
            </p>
          </div>
        </section>

        <section>
          <h2 className="text-xl font-bold text-esmeralda mb-2">Contato</h2>
          <p className="text-sm text-silver">
            Dúvidas, sugestões ou denúncias: use o sistema de denúncias da plataforma ou entre em
            contato pela comunidade.
          </p>
        </section>
      </div>

      <div className="mt-10 text-center">
        <a href="/" className="text-esmeralda hover:underline text-sm">← Voltar ao início</a>
      </div>
    </div>
  );
}
