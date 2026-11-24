// Ações sem JavaScript dentro do HTML (compatível com CSP rigorosa)
document.addEventListener('click', function(e){
  var el = e.target.closest('[data-act]'); if(!el) return;
  var fn = window[el.dataset.act];
  if (typeof fn === 'function') fn(el.dataset.arg);
});
